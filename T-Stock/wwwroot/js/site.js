/* Dark Mode*/
if (document.cookie.includes("darkMode=true")) {
    document.body.classList.add("dark-mode");
}

document.addEventListener('click', function (e) {
    if (e.target && e.target.id === 'darkModeToggle') {
        document.body.classList.toggle('dark-mode');

        document.cookie = "darkMode=" + (document.body.classList.contains("dark-mode")) + ";path=/;max-age=31536000";

    }
});



$(document).on("click", ".ajax-nav", function (e) {
    e.preventDefault();
    const url = $(this).attr("href");

    $("#ajax-content").load(url, function () {
        // After loading, find the <title> inside the partial
        const newTitle = $("#ajax-content").find("title").text();
        if (newTitle) {
            document.title = newTitle; // update browser tab
        }
    });

    return false;

});

