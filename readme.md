# **TUNE – MUSIC PLAYER**
---

## **BESKRIVNING**

Tune är ett musikhanteringssystem som låter användare ladda upp, lagra, spela upp och dela sina egna ljudfiler även sådana som inte finns på större streamingplattformar  

Målet är att skapa en webbaserad tjänst eller mjukvara där användare kan synka sin musik mellan flera enheter och även dela låtar med andra

---

## **PROJEKTBESKRIVNING**

### **PRODUKTMÅL**

Målet med Tune är att skapa ett system där användare kan ladda upp egna ljudfiler, lagra och spela upp musik, synka filer mellan olika enheter och dela musik med andra användare

---

### **MVP (MINIMUM VIABLE PRODUCT)**

#### **Uppladdning av Lokala Musikfiler**
Användaren ska kunna välja och ladda upp ljudfiler från sin enhet

#### **Lista över Uppladdade Filer**
En lista som visar filnamn, längd och grundläggande metadata

#### **Enkel Musikspelare**
Användaren kan spela och pausa musik, använda tidslinje och progress bar samt hoppa i låten

#### **Konto och Inloggning**
Användaren har konto med användarnamn och lösenord samt tvåfaktorsautentisering 2FA

#### **Grundläggande Filsynkning**
Användarens filer ska kunna nås från flera enheter kopplade till samma konto

---

## **ANVÄNDARUPPLEVELSE**

### **Konto och Inloggning**
Användaren skapar ett konto med e-post och lösenord, loggar in genom 2FA och kommer direkt till musikbiblioteket

### **Ladda Upp Musik**
Användaren laddar upp ljudfiler från sin enhet och filerna visas direkt i biblioteket med titel, längd och datum

### **Publicera Musik till Andra**
Användaren kan välja att publicera en låt så att andra användare kan se och spela upp den

### **Spela Musik**
Användaren kan spela och pausa, hoppa i låten, justera volym och se aktuell tidsposition

### **Synka Mellan Enheter**
Vid inloggning på en annan enhet blir alla filer tillgängliga direkt och musikspelaren fungerar likadant på alla enheter

---

## **TEKNISKA LÖSNINGAR**

Här kan diagram, arkitekturbeskrivning och teknikstack läggas till senare


## Authentication (JWT token)

- Backend: added endpoints `POST /auth/register`, `POST /auth/login`, and protected `GET /auth/me`.
- Configure JWT by setting `Jwt` section in `Backend/appsettings.Development.Local.json` (replace `REPLACE_THIS_WITH_A_STRONG_RANDOM_SECRET`).
- The backend issues a JWT access token on successful register/login.

- Frontend: `LoginPage` and `SignUpPage` now call the backend endpoints. Set `BackendApiBaseUrl` in `Frontend/AppConfig/appsettings.Development.Local.json` (default `http://localhost:5000`).
- Tokens are stored in-memory and automatically attached to API requests via `Frontend/Services/ApiClient.cs`.

Notes:
- For production, set `RequireHttpsMetadata = true` in JWT bearer options and store secrets securely (environment variables or secret store).
- Consider adding refresh tokens for long-lived sessions and persistent secure storage for tokens (Windows DPAPI, credential manager, or a secure vault).
