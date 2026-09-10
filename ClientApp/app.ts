
import * as api from "./api.js"
import { formatBytes } from "./helpers.js";
import type { FileBrowserSettings } from "./models.js";
import { searchInput, uploadStatus, uploadLimit, uploadButton, uploadInput, renderError, renderLoading, renderResults, searchForm, clearSearchButton, uploadForm } from "./view.js";
import { navigate } from "./navigation.js";

let settings: FileBrowserSettings | null = null;
let uploading: boolean = false
let deleting: boolean = false;
function getState(): { path: string; query: string } {
    const parameters = new URL(window.location.href).searchParams;
    return {
        path: parameters.get("path") ?? "",
        query: (parameters.get("query") ?? "").trim()
    };
}

async function loadCurrentState(): Promise<void> {
    const { path, query } = getState();

    searchInput.value = query;
    renderLoading(path);

    try {
        const result = query.length > 0
            ? await api.searchDirectory(path, query)
            : await api.browseDirectory(path);

        renderResults(result);
    } catch (error: unknown) {
        renderError(error instanceof Error ? error.message : "An unexpected error has occured.");
    }
}

async function uploadFile(path: string, file: File): Promise<void> {
    uploading = true;
    updateUploadControls();

    uploadStatus.textContent = `Uploading ${file.name}...`

    try {
        const uploadedFile = await api.uploadFile(path, file);
        const destination = path.length === 0 ? "Home" : `Home/${path}`;

        uploadInput.value = "";
        uploadStatus.textContent = `Uploaded ${uploadedFile.name} to ${destination}.`;

        navigate(path, "", true);
    } catch (error: unknown) {
        window.alert(
            error instanceof Error ? error.message : "The upload failed."
        );
    } finally {
        uploading = false;
        uploadInput.value = "";
        updateUploadControls();
    }
}

async function deleteFile(path: string, name: string): Promise<void> {
    if (deleting) return;

    const windowConformation = window.confirm(`Would you like to delete ${name}? \n This will permanently delete the file.`)

    if (!windowConformation) return;

    deleting = true;

    try {
        await api.deleteFile(path);
        await loadCurrentState();
    } catch (error: unknown) {
        window.alert(
            error instanceof Error ? error.message : "The file could not be deleted."
        );
    } finally {
        deleting = false;
    }
}

async function loadSettings(): Promise<void> {
    try {
        settings = await api.getSettings();
        uploadLimit.textContent = `Max file size: ${formatBytes(settings.maximumUploadSizeInBytes)}.`;
    } catch (error: unknown) {
        uploadStatus.textContent = `Uploads are unavailable, try reloading.`
        uploadLimit.textContent = "Upload limit unavailable."
    } finally {
        updateUploadControls()
    }
}
function updateUploadControls(): void {
    const disabled = uploading || settings === null;
    uploadButton.disabled = disabled;
    uploadInput.disabled = disabled;
}

window.addEventListener("navigation", () => {
    void loadCurrentState();
})

window.addEventListener("delete-file", event => {
    if (!(event instanceof CustomEvent)) return;

    const detail = event.detail;

    if (typeof detail?.path !== "string" || typeof detail?.name !== "string")
        return;

    void deleteFile(detail.path, detail.name);
})

window.addEventListener("popstate", () => {
    void loadCurrentState();
})

document.addEventListener("click", event => {
    if (!(event.target instanceof Node)) {
        return;
    }

    if (
        !uploading &&
        !uploadForm.contains(event.target) &&
        !uploadStatus.contains(event.target)
    ) {
        uploadStatus.textContent = "";
    }
});

searchForm.addEventListener("submit", event => {
    event.preventDefault();

    const path = getState().path
    const query = searchInput.value.trim();

    navigate(path, query);
});

clearSearchButton.addEventListener("click", () => {
    navigate(getState().path);
})

uploadForm.addEventListener("submit", event => {
    event.preventDefault();

    if (uploading || settings === null) return;

    const file = uploadInput.files?.[0];

    if (file === undefined) {
        uploadStatus.textContent = "Please select a file to upload.";
        return;
    }

    if (file.size > settings.maximumUploadSizeInBytes) {
        uploadStatus.textContent = `The selected file exceeds the maximum upload size of ${formatBytes(settings.maximumUploadSizeInBytes)}.`;
        uploadInput.value = ""
        return;
    }

    void uploadFile(getState().path, file);

});

updateUploadControls();
void loadSettings();
void loadCurrentState();