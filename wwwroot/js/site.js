window.addEventListener('DOMContentLoaded', () => {
    const alert = document.querySelector('.alert.alert-success'); 
    if (alert) {
        setTimeout(() => {
            alert.style.transition = "opacity 0.3s, margin 0.3s, height 0.3s";
            alert.style.opacity = '0';
            alert.style.margin = '0';
            alert.style.height = '0';
            alert.style.padding = '0';
            alert.style.overflow = 'hidden';

            setTimeout(() => alert.remove(), 400);
        }, 4000);
    }
});
<script>
    const input = document.getElementById('searchInput');
    input.addEventListener('input', function () {
        const query = input.value;

    fetch(`/Clients/Filter?query=${encodeURIComponent(query)}`)
            .then(r => r.text())
            .then(html => {
                document.getElementById('clientTableBody').innerHTML = html;
            });
    });
</script>


