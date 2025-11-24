/* Dark Mode*/
function setTheme() {
    const dark = document.cookie.includes("darkMode=true");
    document.body.classList.toggle("dark-mode", dark);
    document.querySelector(".toggle-ball")?.classList.toggle("move-right", dark);
}

function bindToggle() {
    document.querySelector("#themeToggle")?.addEventListener("click", () => {
        document.body.classList.toggle("dark-mode");
        document.querySelector(".toggle-ball")?.classList.toggle("move-right");

        document.cookie = "darkMode=" + document.body.classList.contains("dark-mode") + "; path=/; max-age=90000000";
    });
}



document.addEventListener("DOMContentLoaded", () => {
    setTheme();
    bindToggle();
});

// AJAX navigation
$(document).on("click", ".ajax-nav", function (e) {
    e.preventDefault();

    $("#ajax-content").load($(this).attr("href"), () => {
        setTheme();
        bindToggle();
    });
});