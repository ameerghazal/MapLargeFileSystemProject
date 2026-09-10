export function navigate(path, query = "", replace = false) {
    const url = new URL(window.location.href);
    if (path.length === 0) {
        url.searchParams.delete("path");
    }
    else {
        url.searchParams.set("path", path);
    }
    const cleanQuery = query.trim();
    if (cleanQuery.length === 0) {
        url.searchParams.delete("query");
    }
    else {
        url.searchParams.set("query", cleanQuery);
    }
    if (replace) {
        window.history.replaceState(null, "", url);
    }
    else {
        window.history.pushState(null, "", url);
    }
    window.dispatchEvent(new Event("navigation"));
}
