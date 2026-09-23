# Document Classifier + Router

Upload a scanned document (invoice, contract, ID, resume, receipt) and the app OCRs it,
then uses an LLM to classify the document type with a confidence score and a plain-language
reason - the kind of pipeline used in real back-office document intake automation.

## Architecture

```
Angular (TypeScript)  --HTTP POST /api/documents/classify-->  ASP.NET Core Web API
      |                                                              |
      | multipart/form-data file upload                              | 1. OCR via Tesseract CLI
      |                                                              |    (+ poppler-utils for PDFs)
      v                                                              |
  Results view                                                       | 2. Classify via OpenAI
  (category, confidence,                                             |    Chat Completions API
   reason, extracted text)  <----------------------------------------+
```

- **Backend** (`backend/DocumentClassifier.Api`): ASP.NET Core 8 Web API.
  - `TesseractOcrService` shells out to the `tesseract` CLI (and `pdftoppm` for PDFs) rather
    than using a native NuGet binding, which keeps the Docker image simple and reliable.
  - `OpenAiClassificationService` calls the OpenAI Chat Completions API directly via
    `HttpClient` with JSON-mode output, asking for a category, confidence, and reason.
  - `DocumentsController` wires the two together behind a single `/api/documents/classify` endpoint.
- **Frontend** (`frontend/`): Angular standalone app (signals, no NgModules) with a drag-and-drop
  upload component and a results view.

## Running locally

**Backend:**
```bash
cd backend/DocumentClassifier.Api
export OpenAI__ApiKey="sk-..."          # or set it in appsettings.Development.json (don't commit it)
dotnet run
```
The API listens on the port ASP.NET Core's dev defaults use (check the console output);
update `frontend/src/environments/environment.ts` if it's not 8080.

Note: OCR needs `tesseract` and `pdftoppm` installed locally too if you're not using Docker
(`brew install tesseract poppler` on macOS, `apt install tesseract-ocr poppler-utils` on Ubuntu).

**Frontend:** see `frontend/SETUP.md` for scaffolding the Angular CLI project around the
provided `src/` files, then `ng serve`.

## Running with Docker

```bash
# Backend
docker build -t document-classifier-api ./backend/DocumentClassifier.Api
docker run -p 8080:8080 -e OpenAI__ApiKey="sk-..." document-classifier-api

# Frontend (after scaffolding per SETUP.md)
docker build -t document-classifier-ui ./document-classifier-ui
docker run -p 4200:8080 document-classifier-ui
```

## Deploying to Render

`render.yaml` at the repo root defines both services as a Render Blueprint:

1. Push this repo to GitHub.
2. In Render: **New > Blueprint**, point it at the repo. Render reads `render.yaml` and
   creates both services automatically.
3. On the `document-classifier-api` service, set the `OpenAI__ApiKey` environment variable
   in the Render dashboard (it's marked `sync: false` in the blueprint so it's never committed).
4. Once the API service is live, copy its `.onrender.com` URL into
   `frontend/src/environments/environment.prod.ts` as `apiUrl`, and update the
   `Cors__AllowedOrigins` value in `render.yaml` (or the dashboard) to match the frontend's
   `.onrender.com` URL. Redeploy both.

Both Dockerfiles bind to Render's `$PORT` environment variable automatically, so no extra
port configuration is needed on Render's side.

## Mapping to the job description

| JD requirement | Where it shows up |
|---|---|
| C# | Entire backend |
| ASP.NET Core & REST API | `DocumentsController`, `Program.cs` |
| Angular & TypeScript | `frontend/src/app/` |
| OpenAI / Azure OpenAI integration | `OpenAiClassificationService` |
| Document AI / OCR | `TesseractOcrService` |
| Debugging, logging, SOLID | `ILogger` throughout, interface-based services (`IOcrService`, `IClassificationService`) for dependency inversion |
| Docker / deployment | Both Dockerfiles + `render.yaml` |

## Extending this further

- Swap `TesseractOcrService` for Azure Document Intelligence's prebuilt models to add
  structured field extraction (line items, totals) on top of classification.
- Store each classified document's text + category in MySQL, then add a search endpoint -
  this turns the project into a searchable, categorized document archive.
- Add a confidence threshold that routes low-confidence results to a "needs review" queue.
