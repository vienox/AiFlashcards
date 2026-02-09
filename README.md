# FlashcardsAI 🎓

An intelligent flashcard application that uses AI to automatically generate flashcards from text or PDF documents. Built with Blazor Server and OpenAI, this app helps you create and study flashcards effortlessly.

## ✨ Features

### 🤖 AI-Powered Generation
- **Automatic flashcard creation** from any text input or PDF document
- Powered by OpenAI (GPT-4o-mini by default)
- Intelligent question-answer pair extraction
- Customizable number of flashcards to generate

### 📚 Deck Management
- Create and organize flashcards into decks
- Save decks to your personal library
- Edit deck names and manage collections
- Track source documents for each deck

### 🎯 Interactive Training Mode
- Flip cards to reveal answers
- Keyboard shortcuts for quick navigation (← → arrow keys)
- Clean, focused study interface
- Track your study progress

### 👤 User Authentication
- Secure user registration and login
- Password-protected accounts
- Personal flashcard library
- User-specific deck management

### 📄 Document Support
- **PDF files**: Automatic text extraction using PdfPig
- **Plain text**: Direct input or paste text
- Efficient document processing with size limits

### 🎨 Modern UI
- Beautiful, responsive design with MudBlazor components
- Gradient background with smooth animations
- Mobile-friendly interface
- Intuitive card flip animations

## 🛠️ Technologies

- **Framework**: ASP.NET Core 8.0 with Blazor Server
- **UI Library**: MudBlazor 8.15.0
- **Database**: SQLite with Entity Framework Core 8.0
- **Authentication**: ASP.NET Core Identity
- **AI Integration**: OpenAI API (gpt-4o-mini)
- **PDF Processing**: UglyToad.PdfPig
- **Language**: C# 12

## 🚀 Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- OpenAI API key ([Get one here](https://platform.openai.com/api-keys))

### Installation

1. **Clone the repository**
   ```bash
   git clone <your-repo-url>
   cd FlashcardsAI
   ```

2. **Set up your OpenAI API key**

   You can configure your API key in one of three ways:

   **Option 1: User Secrets (Recommended for development)**
   ```bash
   dotnet user-secrets init
   dotnet user-secrets set "OpenAI:ApiKey" "your-api-key-here"
   ```

   **Option 2: Environment Variable**
   ```bash
   # Windows (PowerShell)
   $env:OPENAI_API_KEY = "your-api-key-here"
   
   # Linux/macOS
   export OPENAI_API_KEY="your-api-key-here"
   ```

   **Option 3: appsettings.json**
   Add to `appsettings.json` (not recommended for production):
   ```json
   {
     "OpenAI": {
       "ApiKey": "your-api-key-here",
       "Model": "gpt-4o-mini"
     }
   }
   ```

3. **Restore dependencies**
   ```bash
   dotnet restore
   ```

4. **Apply database migrations**
   ```bash
   dotnet ef database update
   ```
   
   Or simply run the app - migrations will be applied automatically.

5. **Run the application**
   ```bash
   dotnet run
   ```

6. **Open your browser**
   Navigate to `https://localhost:5068` (or the URL shown in the console)

## ⚙️ Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=flashcards.db"
  },
  "OpenAI": {
    "ApiKey": "your-api-key-here",
    "Model": "gpt-4o-mini"
  }
}
```

### Creating Flashcards

1. **Log in or create an account**
2. **Choose your input method:**
   - Paste text directly into the text area, OR
   - Upload a PDF file (up to 50MB)
3. **Set the number of flashcards** you want to generate (default: 20)
4. **Click "Generate Flashcards"**
5. **Review the generated cards** - you can flip them to check quality
6. **Save to a deck** by clicking "Save Deck" and entering a name

### Studying Flashcards

1. Navigate to your **saved decks**
2. Click **"Train"** on any deck
3. Use the following controls:
   - **Click the card** or **press Space** to flip
   - **← Left Arrow**: Previous card
   - **→ Right Arrow**: Next card
   - **Escape**: Exit training mode

### Managing Decks

- **Edit deck name**: Click the edit icon next to any deck
- **Delete deck**: Click the delete icon (confirmation required)
- **View deck details**: See card count and source information

## 🏗️ Architecture

### High-Level Overview

```
┌─────────────────────────────────────────────────────────────┐
│                      Blazor Server UI                        │
│  ┌────────────┐  ┌────────────┐  ┌────────────────────┐   │
│  │   Home     │  │   Login    │  │      Train         │   │
│  │   Page     │  │   /Register│  │      Mode          │   │
│  └────────────┘  └────────────┘  └────────────────────┘   │
└──────────────────────┬──────────────────────────────────────┘
                       │
         ┌─────────────┴─────────────┐
         │                           │
         ▼                           ▼
┌──────────────────┐        ┌──────────────────┐
│   Services       │        │  Authentication  │
│  ┌────────────┐  │        │   (Identity)     │
│  │ AI Service │  │        │                  │
│  │  (OpenAI)  │  │        │  User/Password   │
│  ├────────────┤  │        │   Management     │
│  │   Text     │  │        └──────────────────┘
│  │ Extraction │  │
│  ├────────────┤  │
│  │ Flashcard  │  │
│  │   Store    │  │
│  └────────────┘  │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  EF Core         │
│  (ORM)           │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  SQLite          │
│  Database        │
└──────────────────┘
```

### Data Flow

#### 1. Flashcard Generation Flow
```
User Input (Text/PDF)
    ↓
ITextExtractor → Extract text from PDF or use raw text
    ↓
OpenAiFlashcardGenerator → Send to OpenAI API with prompt
    ↓
Parse JSON response → List<Flashcard>
    ↓
Display in UI → User reviews cards
    ↓
User clicks "Save" → FlashcardStore
    ↓
Save to Database → Create Deck + Flashcards
```

#### 2. Study Flow
```
User selects Deck
    ↓
Load Flashcards from Database
    ↓
TrainingState (manages current card index)
    ↓
Display card in Train.razor
    ↓
User interactions:
  - Click/Space → Flip card (CSS animation)
  - Arrow keys → Navigate cards (TrainingState update)
  - ESC → Exit training
```

#### 3. Authentication Flow
```
Login Page → Submit credentials
    ↓
JavaScript (app.js) → POST /account/login
    ↓
SignInManager → Validate credentials
    ↓
Success: Create auth cookie → Redirect to home
Failure: Return error message
```

### Key Components

#### Frontend (Blazor Components)
- **Home.razor**: Main page for creating flashcards
- **Train.razor**: Training mode with card navigation
- **Login/Register.razor**: Authentication forms
- **MainLayout.razor**: App shell with navigation

#### Services Layer
- **OpenAiFlashcardGenerator**: Handles AI integration
  - Builds prompts with system instructions
  - Parses structured JSON responses
  - Handles retries and error cases
  
- **FileTextExtractor**: Extracts text from documents
  - PDF support via PdfPig library
  - Plain text handling
  - File size validation

- **FlashcardStore**: Data access layer
  - CRUD operations for decks and cards
  - User-specific queries
  - Transaction management

- **TrainingState**: Scoped state management for study sessions

#### Data Layer
- **AppDbContext**: Entity Framework Core context
- **Models**: Account, Deck, Flashcard (with relationships)
- **SQLite Database**: Lightweight, file-based storage

### Design Patterns Used

- **Repository Pattern**: FlashcardStore abstracts data access
- **Service Layer Pattern**: Business logic separated from UI
- **Dependency Injection**: All services registered in Program.cs
- **Factory Pattern**: AppDbContextFactory for migrations
- **State Management**: Scoped services for user sessions

### Security Architecture

- **ASP.NET Core Identity**: Industry-standard authentication
- **Cookie-based authentication**: Secure, HTTP-only cookies
- **Password hashing**: PBKDF2 with salt
- **CSRF protection**: Built-in antiforgery tokens
- **Authorization**: [Authorize] attributes on protected pages

## 📁 Project Structure

```
FlashcardsAI/
├── Components/
│   ├── Auth/               # Authentication components
│   ├── Layout/             # Layout components (MainLayout)
│   ├── Pages/              # Blazor pages (Home, Login, Train)
│   └── Shared/             # Shared components (dialogs)
├── Data/
│   ├── AppDbContext.cs     # EF Core database context
│   └── AppDbContextFactory.cs
├── Migrations/             # EF Core migrations
├── Models/                 # Data models (Account, Deck, Flashcard)
├── Services/
│   ├── Ai/                 # OpenAI integration
│   ├── Auth/               # Authentication services
│   ├── Data/               # Data access layer
│   ├── TextExtraction/     # PDF and text processing
│   └── Training/           # Training mode state management
├── wwwroot/
│   ├── app.css             # Custom styles
│   ├── app.js              # JavaScript interop
│   └── bootstrap/          # Bootstrap CSS
├── Program.cs              # Application entry point
└── appsettings.json        # Configuration
```

## 🔐 Security Notes

- **Never commit API keys** to version control
- Use **User Secrets** for development
- Use **environment variables** or secure vaults in production
- The app uses **ASP.NET Core Identity** for secure authentication
- Passwords are hashed with industry-standard algorithms

## 🐛 Troubleshooting

### "Missing OpenAI API key" error
Make sure you've set your API key using one of the methods described in the Installation section.

### Database errors
Try deleting `flashcards.db` and running the app again to recreate the database.

### PDF extraction fails
Ensure your PDF is not password-protected and contains extractable text (not scanned images).

## 📝 License

This project is provided as-is for educational and personal use.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

## 🙏 Acknowledgments

- **MudBlazor** for the amazing UI components
- **OpenAI** for the GPT API
- **PdfPig** for PDF text extraction
- **ASP.NET Core team** for the excellent framework

---
