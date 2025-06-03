document.addEventListener('DOMContentLoaded', function () {
    const video = document.querySelector('.hero-video');
    const toggleBtn = document.getElementById('videoToggleBtn');

    if (video && toggleBtn) {
        toggleBtn.addEventListener('click', function () {
            if (video.paused) {
                video.play();
                toggleBtn.innerHTML = `<svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 32 32"><path d="M11 26L11 6H12L12 26H11ZM20 26L20 6H21L21 26H20Z" fill="currentColor"></path></svg>`;
                toggleBtn.setAttribute('aria-label', 'Pause video');
            } else {
                video.pause();
                toggleBtn.innerHTML = `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M8 5.14v14l11-7-11-7z" fill="currentColor"></path></svg>`;
                toggleBtn.setAttribute('aria-label', 'Play video');
            }
        });
    }

    const crudSection = document.getElementById('crudSection');
    const showAdminLoginBtn = document.getElementById('showAdminLoginBtn');
    const adminLoginForm = document.getElementById('adminLoginForm');
    const adminLoginError = document.getElementById('adminLoginError');

    function checkAdminLogin() {
        if (localStorage.getItem('isAdmin') === 'true') {
            crudSection.style.display = '';
            showAdminLoginBtn.style.display = 'none';
        } else {
            crudSection.style.display = 'none';
            showAdminLoginBtn.style.display = '';
        }
    }

    checkAdminLogin();

    adminLoginForm.onsubmit = function (e) {
        e.preventDefault();
        const user = document.getElementById('adminUser').value;
        const pass = document.getElementById('adminPass').value;

        // Giả lập đăng nhập admin (có thể thay bằng gọi API)
        if (user === 'admin' && pass === '123') {
            localStorage.setItem('isAdmin', 'true');
            adminLoginError.textContent = '';
            checkAdminLogin();
            const modal = bootstrap.Modal.getInstance(document.getElementById('adminLoginModal'));
            if (modal) modal.hide();
        } else {
            adminLoginError.textContent = 'Sai tài khoản hoặc mật khẩu!';
        }
    };

    const logoutBtn = document.createElement('button');
    logoutBtn.textContent = 'Đăng xuất';
    logoutBtn.className = 'btn btn-link text-danger float-end';
    logoutBtn.onclick = function () {
        localStorage.removeItem('isAdmin');
        checkAdminLogin();
    };
    crudSection.prepend(logoutBtn);

    // Dữ liệu giả lập để test CRUD
    const products = [
        { name: "Ultraboost 22", price: 1900000, imageUrl: "https://via.placeholder.com/100" },
        { name: "Stan Smith", price: 900000, imageUrl: "https://via.placeholder.com/100" }
    ];

    function renderProducts() {
        const productList = document.getElementById('productList');
        productList.innerHTML = '';
        if (products.length === 0) {
            productList.innerHTML = `<tr><td colspan="4" class="text-center text-muted">Chưa có sản phẩm nào</td></tr>`;
            return;
        }
        products.forEach((p, idx) => {
            const tr = document.createElement('tr');
            tr.innerHTML = `
                <td><b>${p.name}</b></td>
                <td>${p.price.toLocaleString('vi-VN')}₫</td>
                <td><img src="${p.imageUrl}" width="100" /></td>
                <td><button onclick="alert('Chức năng đang phát triển')" class="btn btn-outline-primary btn-sm me-2">Sửa</button></td>
            `;
            productList.appendChild(tr);
        });
    }

    renderProducts();
});
