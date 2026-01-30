using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using MySql.Data.MySqlClient;
using System.Security.Claims;
using NAudio.Wave;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/songs")]
    public class SongsController : ControllerBase
    {
        private readonly string _connectionString;

        public SongsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MariaDB");
        }

        [Authorize]
        [HttpPost("upload")]
        public async Task<IActionResult> UploadSong([FromForm] IFormFile mp3_file, [FromForm] string NameOfSong, [FromForm] IFormFile? thumbnail_file = null)
        {
            try
            {
                if (mp3_file == null || mp3_file.Length == 0)
                    return BadRequest("No file uploaded.");

                // Grab user ID from token so we know who owns this song
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst(ClaimTypes.Name);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int creatorUserID))
                    return Unauthorized("User ID not found in token.");

                // Read file into byte array
                byte[] mp3Bytes;
                using (var ms = new MemoryStream())
                {
                    await mp3_file.CopyToAsync(ms);
                    mp3Bytes = ms.ToArray();
                }

                // Read thumbnail if provided
                byte[]? thumbnailBytes = null;
                if (thumbnail_file != null && thumbnail_file.Length > 0)
                {
                    using (var ms = new MemoryStream())
                    {
                        await thumbnail_file.CopyToAsync(ms);
                        thumbnailBytes = ms.ToArray();
                    }
                }

                // Use NAudio to get the song duration
                double length = 0;
                using (var mp3Stream = new MemoryStream(mp3Bytes))
                using (var reader = new Mp3FileReader(mp3Stream))
                {
                    length = reader.TotalTime.TotalSeconds;
                }

                // Store song in database with file data
                using (var conn = new MySqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    var cmd = conn.CreateCommand();
                    if (thumbnailBytes != null && thumbnailBytes.Length > 0)
                    {
                        cmd.CommandText = "INSERT INTO songs (NameOfSong, length, creatorUserID, mp3_file, thumbnail) VALUES (@name, @length, @creator, @mp3, @thumbnail)";
                        cmd.Parameters.AddWithValue("@thumbnail", thumbnailBytes);
                    }
                    else
                    {
                        cmd.CommandText = "INSERT INTO songs (NameOfSong, length, creatorUserID, mp3_file) VALUES (@name, @length, @creator, @mp3)";
                    }
                    cmd.Parameters.AddWithValue("@name", NameOfSong);
                    cmd.Parameters.AddWithValue("@length", length);
                    cmd.Parameters.AddWithValue("@creator", creatorUserID);
                    cmd.Parameters.AddWithValue("@mp3", mp3Bytes);
                    await cmd.ExecuteNonQueryAsync();
                }

                return Ok(new { message = "Song uploaded successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [Authorize]
        [HttpGet("my-songs")]
        public async Task<IActionResult> GetMySongs()
        {
            try
            {
                // Figure out which user is requesting their library
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst(ClaimTypes.Name);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int creatorUserID))
                    return Unauthorized("User ID not found in token.");

                var songs = new List<object>();
                using (var conn = new MySqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT id, NameOfSong, length FROM songs WHERE creatorUserID = @userId";
                    cmd.Parameters.AddWithValue("@userId", creatorUserID);
                    
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            songs.Add(new
                            {
                                id = reader.GetInt32(0),
                                nameOfSong = reader.GetString(1),
                                length = reader.GetDouble(2)
                            });
                        }
                    }
                }

                return Ok(songs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllSongs()
        {
            try
            {
                var songs = new List<object>();
                using (var conn = new MySqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT s.id, s.NameOfSong, s.length, u.username, s.thumbnail FROM songs s JOIN users u ON s.creatorUserID = u.userID";

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            songs.Add(new
                            {
                                Id = reader.GetInt32(0),
                                NameOfSong = reader.GetString(1),
                                Length = reader.GetDouble(2),
                                CreatorUsername = reader.GetString(3),
                                ThumbnailBytes = reader.IsDBNull(4) ? Array.Empty<byte>() : (byte[])reader.GetValue(4)
                            });
                        }
                    }
                }

                return Ok(songs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSong(int id)
        {
            try
            {
                // Verify they own the song before deleting
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst(ClaimTypes.Name);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int creatorUserID))
                    return Unauthorized("User ID not found in token.");

                using (var conn = new MySqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    var cmd = conn.CreateCommand();
                    // This only works if the song belongs to them
                    cmd.CommandText = "DELETE FROM songs WHERE id = @id AND creatorUserID = @userId";
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@userId", creatorUserID);
                    
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                        return NotFound("Song not found or you don't have permission to delete it.");
                }

                return Ok(new { message = "Song deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [Authorize]
        [HttpGet("{id}/play")]
        public async Task<IActionResult> GetSongFile(int id)
        {
            try
            {
                byte[]? mp3Bytes = null;
                string? fileName = null;
                using (var conn = new MySqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT mp3_file, NameOfSong FROM songs WHERE id = @id";
                    cmd.Parameters.AddWithValue("@id", id);
                    
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            mp3Bytes = (byte[])reader.GetValue(0);
                            fileName = reader.GetString(1);
                        }
                    }
                }

                if (mp3Bytes == null)
                    return NotFound("Song not found.");

                return File(mp3Bytes, "audio/mpeg", $"{fileName}.mp3");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
    }
}
