/* Dark Mode*/
if (document.cookie.includes("darkMode=true")) {
    document.body.classList.add("dark-mode");
}

const toggle = document.getElementById('darkModeToggle');

toggle.addEventListener('click', () => {
    document.body.classList.toggle('dark-mode');

    if (document.body.classList.contains('dark-mode')) {
        document.cookie = "darkMode=true; path=/; max-age=31536000";
    } else {
        document.cookie = "darkMode=false; path=/; max-age=31536000";
    }
});


