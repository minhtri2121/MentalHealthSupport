// Back to Top Button
window.addEventListener('scroll', function () {
    var backToTop = document.querySelector('.back-to-top');
    if (window.scrollY > 300) { // Thay pageYOffset bằng scrollY
        backToTop.classList.add('show');
    } else {
        backToTop.classList.remove('show');
    }
});

document.querySelector('.back-to-top').addEventListener('click', function (e) {
    e.preventDefault();
    window.scrollTo({
        top: 0,
        behavior: 'smooth'
    });
});
