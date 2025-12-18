/* =========================================================
   1. Configuration Helper
   ========================================================= */
function getConfig() {
    return window.inventoryConfig || {
        currentPage: 1,
        pageSize: 10,
        sortBy: "ProductName",
        sortDir: "asc"
    };
}

/* =========================================================
   2. Dark Mode Logic
   ========================================================= */
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

/* =========================================================
   3. Inventory Loading & Pagination
   ========================================================= */
function loadInventory(page, callback) {
    const config = getConfig();
    const targetPage = page ? Number(page) : Number(config.currentPage);

    if (window.inventoryConfig) {
        window.inventoryConfig.currentPage = targetPage;
    }

    // Standard Inventory Load
    const url = `/Inventory/Index?page=${targetPage}&pageSize=${config.pageSize}&sortBy=${config.sortBy}&sortDir=${config.sortDir} #inventory-table`;

    $("#inventory-table").load(url, function (response, status, xhr) {
        if (status === "error") {
            console.log("Error loading inventory");
        } else {
            bindSearchResultLinks();
            if (callback) callback();
        }
    });
}

// Pagination & Sorting Event Listeners
$(document).on("click", ".page-btn", function () {
    const page = $(this).data("page");
    if (page) loadInventory(page);
});

$(document).on("change", "#sortBy", function () {
    if (window.inventoryConfig) {
        window.inventoryConfig.sortBy = $(this).val();
        loadInventory(window.inventoryConfig.currentPage);
    }
});

/* Sorting - Direction Button */
$(document).on("click", "#sortDirBtn", function (e) {
    e.preventDefault();
    if (window.inventoryConfig) {
        const newDir = window.inventoryConfig.sortDir === "asc" ? "desc" : "asc";
        window.inventoryConfig.sortDir = newDir;
        $(this).attr("data-dir", newDir);

        const icon = $(this).find("i");

        // 1. CLEAR all arrow classes first (including the initial 'up-down' one)
        icon.removeClass("ri-arrow-up-down-line ri-arrow-up-line ri-arrow-down-line");

        // 2. ADD the correct class based on direction
        if (newDir === "asc") {
            icon.addClass("ri-arrow-up-line");
        } else {
            icon.addClass("ri-arrow-down-line");
        }

        loadInventory(window.inventoryConfig.currentPage);
    }
});

/* =========================================================
   4. SEARCH FUNCTIONALITY (Fixed)
   ========================================================= */

// Input Listener
$(document).on("keyup", "#searchBox", function () {
    let query = $(this).val().trim();

    if (query === "") {
        $("#search-results").empty().hide();
        return;
    }

    const config = getConfig();

    // UPDATED: Points to /Home/Search as requested
    $.get("/Home/Search", { q: query, pageSize: config.pageSize }, function (data) {
        if (!data || data.trim() === "") {
            $("#search-results").empty().hide();
        } else {
            $("#search-results").html(data).show();
            // Re-bind clicks to the new results
            bindSearchResultLinks();
        }
    });
});

// Hide results on outside click
$(document).on("click", function (e) {
    if (!$(e.target).closest(".inven-search-container, .search-container").length) {
        $("#search-results").hide();
    }
});

/* ---------------------------------------------------------
   Logic: Calculate Page -> Navigate -> Highlight
   --------------------------------------------------------- */

// LOGIC: Calculate which page a product is on based on current sort
function getPageForProduct(productId) {
    // Requires window.allProducts to be set in Index.cshtml
    const allProducts = window.allProducts || window.inventoryConfig?.products || [];
    const config = getConfig();

    if (!allProducts.length) return 1;

    // HELPER: Safely get property value regardless of casing (ProductId vs productId)
    const getValue = (item, prop) => {
        // Try PascalCase (C# default)
        if (item[prop] !== undefined) return item[prop];
        // Try camelCase (JSON default)
        const camel = prop.charAt(0).toLowerCase() + prop.slice(1);
        if (item[camel] !== undefined) return item[camel];
        return null;
    };

    // 1. Sort the full list exactly like the server does
    const sortedProducts = [...allProducts].sort((a, b) => {
        let valA = getValue(a, config.sortBy);
        let valB = getValue(b, config.sortBy);

        // Handle nulls safely
        if (valA == null) valA = "";
        if (valB == null) valB = "";

        // Compare strings case-insensitively
        if (typeof valA === "string") valA = valA.toLowerCase();
        if (typeof valB === "string") valB = valB.toLowerCase();

        // Direction logic
        if (valA < valB) return config.sortDir === "asc" ? -1 : 1;
        if (valA > valB) return config.sortDir === "asc" ? 1 : -1;
        return 0;
    });

    // 2. Find index using the safe getter
    const index = sortedProducts.findIndex(p => {
        const pId = getValue(p, "ProductId");
        // Use loose equality (==) to handle number vs string ID mismatches
        return pId == productId;
    });

    if (index < 0) return 1;

    // 3. Calculate Page
    return Math.floor(index / config.pageSize) + 1;
}

function bindSearchResultLinks() {
    const searchLinks = document.querySelectorAll('.search-result-link');

    searchLinks.forEach(link => {
        // Use cloning to ensure we don't stack event listeners
        const newLink = link.cloneNode(true);
        link.parentNode.replaceChild(newLink, link);

        newLink.addEventListener('click', function (e) {
            e.preventDefault();

            const productId = this.dataset.productId;

            // Calculate Page (Client Side)
            const targetPage = getPageForProduct(productId);

            // Check if we are currently on the Inventory Page
            const inventoryTable = document.getElementById("inventory-table");

            if (inventoryTable) {
                // SCENARIO A: Already on Inventory Page -> AJAX Reload
                $("#search-results").hide();
                $("#searchBox").val(""); // Clear search box

                loadInventory(targetPage, function () {
                    scrollToProduct(productId);
                });
            } else {
                // SCENARIO B: On Dashboard -> Redirect
                // We pass the calculated page in the URL so the server loads it immediately
                window.location.href = `/Inventory/Index?page=${targetPage}#product-${productId}`;
            }
        });
    });
}

function scrollToProduct(productId) {
    const rowId = `#product-${productId}`;
    const row = document.querySelector(rowId);

    if (row) {
        row.scrollIntoView({ behavior: "smooth", block: "start" });
        row.classList.add("highlight");
        setTimeout(() => row.classList.remove("highlight"), 2000);
    }
}

// Handle Hash on Page Load (For Scenario B Redirects)
function highlightHashRow() {
    const hash = window.location.hash;
    if (!hash) return;

    setTimeout(() => {
        const row = document.querySelector(hash);
        if (row) {
            row.scrollIntoView({ behavior: "smooth", block: "start" });
            row.classList.add("highlight");
            setTimeout(() => row.classList.remove("highlight"), 2000);

            // Optional: Remove hash to clean URL
            history.replaceState(null, null, ' ');
        }
    }, 500);
}

/* =========================================================
   5. Initialization
   ========================================================= */
document.addEventListener("DOMContentLoaded", () => {
    // 1. Sync global data if View provided it
    if (window.inventoryData) {
        window.allProducts = window.inventoryData;
    }

    // 2. Initial Setup
    setTheme();
    bindToggle();
    bindSearchResultLinks();
    highlightHashRow();
});

window.addEventListener("hashchange", highlightHashRow);

/* =========================================================
   6. AJAX Navigation (Sidebar)
   ========================================================= */
// Load page content via AJAX
function loadPage(url) {
    $("#ajax-content").load(url, function () {
        setTheme();
        bindToggle();
        // Re-initialize specific page logic if needed
        if (window.inventoryData) window.allProducts = window.inventoryData;
    });
}

$(document).on("click", ".ajax-nav", function (e) {
    e.preventDefault();
    $("#ajax-content").load($(this).attr("href"), () => {
        setTheme();
        bindToggle();
    });
});

$(document).on("click", "a.ajax-nav[data-ajax='page']", function (e) {
    e.preventDefault();
    const url = $(this).attr("href");
    loadPage(url);
    history.pushState({ pageUrl: url }, '', url);
});

// Back/Forward buttons
window.onpopstate = function (event) {
    if (event.state && event.state.pageUrl) {
        loadPage(event.state.pageUrl);
    }
};
history.replaceState({ pageUrl: window.location.href }, '', window.location.href);

/* =========================================================
   7. Row Management (Add/Remove)
   ========================================================= */
let rowIndex = 1;
function addRow() {
    const table = document.getElementById("item-rows");
    if (!table) return;
    const row = document.createElement("tr");
    row.classList.add("adding");
    row.innerHTML = `
        <td><input name="Items[${rowIndex}].ProductName" class="form-control" /></td>
        <td><input name="Items[${rowIndex}].Category" class="form-control" /></td>
        <td><input name="Items[${rowIndex}].Quantity" type="number" class="form-control" value="0" /></td>
        <td><input name="Items[${rowIndex}].ReorderLevel" type="number" class="form-control" value="0" /></td>
        <td><input name="Items[${rowIndex}].Price" type="number" class="form-control" value="0" /></td>
        <td><button type="button" onclick="removeRow(this)" class="button">Remove</button></td>
    `;
    table.appendChild(row);
    rowIndex++;
    requestAnimationFrame(() => row.classList.add("show"));
}

function removeRow(btn) {
    const row = btn.closest("tr");
    const table = document.getElementById("item-rows");
    if (row === table.querySelector("tr:first-child")) return;
    row.classList.add("removing");
    row.remove();
}

if (typeof $ !== 'undefined' && $.validator && $.validator.unobtrusive) {
    $.validator.unobtrusive.parse('#item-rows');
}