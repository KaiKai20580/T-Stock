/* Dark Mode*/
function setTheme() {
    const dark = document.cookie.includes("darkMode=true");
    document.body.classList.toggle("dark-mode", dark);
    document.querySelector(".toggle-ball")?.classList.toggle("move-right", dark);
}

function bindToggle() {
    document.querySelector("#themeToggle").addEventListener("click", () => {
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

//Test



// AJAX for button
$(document).on("click", ".ajax-button", function (e) {
    e.preventDefault();
    let form = $(this).closest("form");

    $.ajax({
        url: form.attr("action"),
        method: "POST",
        data: form.serialize(),
        success: function (res) {
            if (res.success) {
                window.location.href = "/Inventory/Index";
            } else {
                $("#ajax-content").html(res);
                $.validator.unobtrusive.parse("#ajax-content");
            }
        }
    });
});



let rowIndex = 1;

function addRow() {
    const table = document.getElementById("item-rows");
    const row = document.createElement("tr");

    row.innerHTML = `
        <td><input name="Items[${rowIndex}].ItemName" class="form-control" /></td>
        <td><input name="Items[${rowIndex}].Category" class="form-control" /></td>
        <td><input name="Items[${rowIndex}].Quantity" type="number" class="form-control" /></td>
        <td><input name="Items[${rowIndex}].Price" type="number" class="form-control" /></td>
        <td><button type="button" onclick="removeRow(this)">Remove</button></td>
    `;

    table.appendChild(row);
    rowIndex++;
}

function removeRow(btn) {
    const row = btn.closest("tr");
    const table = document.getElementById("item-rows");

    if (row === table.querySelector("tr:first-child")) return;
    btn.closest("tr").remove();
}

$.validator.unobtrusive.parse('#item-rows');