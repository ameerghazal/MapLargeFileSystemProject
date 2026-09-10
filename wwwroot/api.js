async function apiRequest(url, options) {
    const headers = new Headers(options?.headers);
    headers.set("Accept", "application/json");
    const response = await fetch(url, { ...options, headers });
    if (!response.ok) {
        let message = `API request failed with status ${response.status}.`;
        try {
            const body = await response.json();
            if (typeof body?.error === "string")
                message = body.error;
        }
        catch { }
        throw new Error(message);
    }
    if (response.status === 204)
        return undefined;
    return await response.json();
}
export function browseDirectory(path) {
    const param = new URLSearchParams({ path });
    return apiRequest(`/api/files/browse?${param}`);
}
export function searchDirectory(path, query) {
    const param = new URLSearchParams({ path, query });
    return apiRequest(`/api/files/search?${param}`);
}
export function uploadFile(path, file) {
    const formData = new FormData();
    formData.append("file", file);
    const param = new URLSearchParams({ path });
    return apiRequest(`/api/files/upload?${param}`, {
        method: "POST",
        body: formData
    });
}
export function deleteFile(path) {
    const param = new URLSearchParams({ path });
    return apiRequest(`/api/files/delete?${param}`, { method: "DELETE" });
}
export function getSettings() {
    return apiRequest("/api/files/settings");
}
