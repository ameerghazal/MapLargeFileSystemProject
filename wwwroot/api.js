async function apiRequest(url, options) {
    const headers = new Headers(options?.headers);
    headers.set("Accept", "application/json");
    const response = await fetch(url, { ...options, headers });
    if (!response.ok) {
        throw new Error(`API request failed with status ${response.status}`);
    }
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
export function getSettings() {
    return apiRequest("/api/files/settings");
}
