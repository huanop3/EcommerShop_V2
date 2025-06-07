function setCookie(name, value) {
    // Convert the C# DateTime to a JavaScript date format
    document.cookie = `${name}=${value}; path=/; secure=true;`;
}
function deleteCookie(name) {
    document.cookie = name + '=; Path=/; Expires=Thu, 01 Jan 1970 00:00:01 GMT;';
}
// Make the function available globally
window.setCookie = setCookie;
window.deleteCookie = deleteCookie;