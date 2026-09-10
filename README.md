# MapLarge File Browser

A single-page file browser built with ASP.NET Core (.NET 8), C#, HTML, CSS, and vanilla TypeScript.

The application works with real files inside a configurable server directory. When running locally, this is a directory on the computer running the backend. The default directory is `MockDirectory`, but it can be set in the appsettings, as needed.

## Features

- Browse files and folders, with folders listed first and names sorted alphabetically.
- Navigate using folder buttons and breadcrumbs.
- Search file and folder names recursively within the current directory, ignoring case.
- Preserve the current directory and search query in the URL, including refresh and browser Back/Forward navigation.
- Display file counts, folder counts, and total file size for the current results.
- Upload individual files without overwriting existing items.
- Download individual files.
- Delete individual files after confirmation (for the bonus challenge).
- Display loading, empty-result, and error states (to an extent).

Search results include relative paths to distinguish matching names in different folders. Counts and total size reflect the returned items.

## Requirements

- .NET 8 SDK
- Node.js and npm for compiling TypeScript
- A modern browser
- Optional: Visual Studio 2022 with .NET 8 support and the ASP.NET and web development workload

## Running the Project

Open a terminal in the directory containing `TestProject.csproj` and `package.json`:

```sh
npm ci
npx tsc
dotnet restore
dotnet run --launch-profile TestProject
```

Open:

```text
https://localhost:7146/index.html
```

If the local HTTPS certificate is not trusted, run:

```sh
dotnet dev-certs https --trust
```

### Using Visual Studio

1. Open `TestProject.sln`.
2. Run `npm ci` and `npx tsc` from the project directory.
3. Select the **TestProject** launch profile.
4. Press F5 to start debugging.

The project uses Kestrel. IIS Express is not required.

## Frontend Development

Edit the TypeScript files in `ClientApp`, then compile them into JavaScript in `wwwroot`:

```sh
npx tsc
```

To compile automatically while editing:

```sh
npx tsc --watch
```

Must run npx tsc when a change is made to a typescript file! This will compile them into JS.

TypeScript compilation is separate from the .NET build. Recompile after frontend changes and include the generated JavaScript when submitting the project.

Open the application through ASP.NET Core rather than opening `index.html` directly from disk.

## Configuration

Update the `FileBrowser` section in `appsettings.json`:

```json
"FileBrowser": {
  "RootPath": "MockDirectory",
  "MaximumUploadSizeInBytes": 25000000,
  "MaximumSearchResults": 500
}
```
Root can be set here, along with some thresholds I included. More could be added here in the future, if need be.

| Setting | Description |
| --- | --- |
| `RootPath` | Server directory exposed by the application. Relative paths resolve against the application's content root. |
| `MaximumUploadSizeInBytes` | Maximum accepted file size. The default is 25 MB. |
| `MaximumSearchResults` | Maximum number of returned search items, including files and folders. The default is 500. |

Restart the application after changing settings.

The root directory is created if it does not exist. The backend needs permission to read files and write uploaded files within it. The frontend retrieves the upload and search limits from the settings endpoint.

Displayed sizes use decimal units: 1 KB = 1,000 bytes and 1 MB = 1,000,000 bytes.

Hosting and multipart request limits are separate from the file-size setting. Review those limits if increasing the upload maximum.

## Project Structure

| File or Directory | Responsibility |
| --- | --- |
| `Program.cs` | Configuration validation, dependency injection, static files, and controller routing |
| `Controllers/FileController.cs` | API endpoints and HTTP error responses |
| `Services/IFileSystemService.cs` | Contract for filesystem operations |
| `Services/FileSystemService.cs` | Browse, search, upload, download, delete, and shared mapping/sorting/summary helpers |
| `Services/FilePathResolve.cs` | Root-relative path resolution and symbolic-link checks |
| `Models/FileBrowserModel.cs` | C# response records and download data |
| `Options/FileBrowserOptions.cs` | Configuration defaults |
| `ClientApp/app.ts` | Application startup, state, form events, and upload/delete coordination |
| `ClientApp/api.ts` | API calls and shared response/error handling |
| `ClientApp/navigation.ts` | URL updates and navigation events |
| `ClientApp/view.ts` | DOM elements, table rows, breadcrumbs, and status messages |
| `ClientApp/helpers.ts` | File-size, date, and count formatting |
| `ClientApp/models.ts` | TypeScript response interfaces |
| `wwwroot` | HTML, CSS, and compiled JavaScript modules |
| `MockDirectory` | Default directory used for browsing and uploads |

## API Endpoints

All API paths are relative to the configured root. An omitted or empty directory path represents Home.

| Method | Endpoint | Purpose |
| --- | --- | --- |
| GET | `/api/files/settings` | Retrieve upload and search limits |
| GET | `/api/files/browse?path=Documents` | List direct children of a directory |
| GET | `/api/files/search?path=Documents&query=report` | Search names recursively beneath a directory |
| GET | `/api/files/download?path=Documents/report.txt` | Download a file |
| POST | `/api/files/upload?path=Documents` | Upload multipart form data with a field named `file` |
| DELETE | `/api/files/delete?path=Documents/report.txt` | Delete a file and return HTTP 204 on success |

Example frontend URL:

```text
https://localhost:7146/index.html?path=Documents&query=report
```

## Implementation Notes

- The filesystem is the data source; no database or frontend framework is required.
- Paths are validated against the configured root. Detected symbolic links and reparse points are rejected or excluded.
- Browse and search reuse item mapping, sorting, and summary helpers.
- Uploads use `FileMode.CreateNew` to avoid overwriting existing files. Failed uploads attempt to remove partially written files.
- Search retrieves one extra match to determine whether the results were truncated.
- Navigation updates the URL and emits an event that the application handles, avoiding circular imports between frontend modules.
- Deletion requires confirmation in the UI, and the backend explicitly rejects directory deletion.

## If I Were Continuing This Project

- Add a trash folder and restore option so deleted files can be recovered.
- Support moving, copying, and renaming files and folders.
- Add folder uploads while preserving the directory structure.
- Add unit tests for path validation, search behavior, and file operations, along with integration tests for the API.
- Prevent older requests from replacing the results of more recent navigation.
- Improve error messages for duplicate uploads and other file conflicts.
