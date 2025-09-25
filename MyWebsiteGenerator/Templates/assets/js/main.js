document.addEventListener('DOMContentLoaded', function () {
    // 1. Lógica para el 'highlight' del menú de navegación.
    try {
        const currentPath = window.location.pathname;
        const currentFile = currentPath.split('/').pop();
        const navLinks = document.querySelectorAll('.nav-link');

        navLinks.forEach(link => {
            const href = link.getAttribute('href');
            if (href === currentFile || (href === 'about.html' && currentFile === '')) {
                link.classList.add('active');
            }
        });
    } catch (e) {
        console.error("Error al aplicar highlight en la navegación: ", e);
    }

    // 2. Lógica para mostrar/ocultar la genealogía
    const toggleButton = document.getElementById('toggleGenealogy');
    const genealogySection = document.getElementById('genealogySection');
    if (toggleButton && genealogySection) {
        toggleButton.addEventListener('click', () => {
            if (genealogySection.classList.contains('d-none')) {
                genealogySection.classList.remove('d-none');
                toggleButton.textContent = 'Ocultar mi genealogía';
            } else {
                genealogySection.classList.add('d-none');
                toggleButton.textContent = 'Mostrar mi genealogía';
            }
        });
    }

    // 3. Validación simple del formulario de contacto.
    const forms = document.querySelectorAll('form.needs-validation');
    forms.forEach(form => {
        form.addEventListener('submit', function (e) {
            const name = form.querySelector('#name');
            const email = form.querySelector('#email');
            const message = form.querySelector('#message');

            if (!name || !email || !message) {
                return;
            }

            let isValid = true;
            if (name.value.trim() === '' || email.value.trim() === '' || message.value.trim() === '') {
                isValid = false;
            }

            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            if (!emailRegex.test(email.value.trim())) {
                alert('Por favor, ingresa un correo electrónico válido.');
                e.preventDefault();
                return;
            }

            if (!isValid) {
                alert('Por favor, completa todos los campos obligatorios.');
                e.preventDefault();
            } else {
                alert('Mensaje enviado (simulación). ¡Gracias!');
                e.preventDefault();
            }
        });
    });

    // 4. Manejo de errores de carga de imágenes.
    document.querySelectorAll('img').forEach(img => {
        img.addEventListener('error', () => {
            img.src = '../assets/img/placeholder.png';
            img.alt = 'Imagen no disponible';
        });
    });

    // 5. Lógica de interactividad para la línea de tiempo (opcional)
    document.querySelectorAll('.timeline-item-modern').forEach(item => {
        item.addEventListener('click', () => {
            document.querySelectorAll('.timeline-item-modern').forEach(i => i.classList.remove('selected-item'));
            item.classList.add('selected-item');
        });
    });

    //Lógica para mostrar/ocultar la galería de hobbies
    const galleryToggleButtons = document.querySelectorAll('.toggle-gallery-btn');
    galleryToggleButtons.forEach(button => {
        button.addEventListener('click', () => {
            const cardBody = button.closest('.card');
            const gallery = cardBody.querySelector('.hobby-gallery');
            if (gallery.classList.contains('visible')) {
                gallery.classList.remove('visible');
                button.textContent = 'Ver Galería';
            } else {
                gallery.classList.add('visible');
                button.textContent = 'Ocultar Galería';
            }
        });
    });


    //Lógica para mostrar/ocultar la descripción de YouTubers
    const toggleDescriptionButtons = document.querySelectorAll('.toggle-description-btn');
    toggleDescriptionButtons.forEach(button => {
        button.addEventListener('click', () => {
            const cardBody = button.closest('.card');
            const descriptionContainer = cardBody.querySelector('.description-container');

            if (descriptionContainer.classList.contains('d-none')) {
                descriptionContainer.classList.remove('d-none');
                button.textContent = 'Ocultar Descripción';
            } else {
                descriptionContainer.classList.add('d-none');
                button.textContent = 'Ver Descripción';
            }
        });
    })


    // --- LÓGICA PARA EL MODAL DE DETALLE DE SERIES ---
    const detailModal = document.getElementById('detailModal');
    if (detailModal) {
        detailModal.addEventListener('show.bs.modal', event => {
            const cardElement = event.relatedTarget; // La miniatura que se hizo clic

            // 1. Obtener datos del data-* attributes de la miniatura
            const nombre = cardElement.getAttribute('data-nombre');
            const descripcion = cardElement.getAttribute('data-descripcion');
            const caratulaUrl = cardElement.getAttribute('data-caratula');
            const trailerUrl = cardElement.getAttribute('data-trailer-url');

            // 2. Referencias a los elementos del modal
            const modalTitle = document.getElementById('detailModalLabel');
            const detailCaratula = document.getElementById('detailCaratula');
            const detailDescription = document.getElementById('detailDescription');
            const youtubeIframe = document.getElementById('youtubeIframe');

            // 3. Llenar el modal
            modalTitle.textContent = nombre;
            detailCaratula.src = caratulaUrl;
            detailDescription.textContent = descripcion;

            // 4. Cargar el trailer de YouTube
            function getYouTubeId(url) {
                // Regex para extraer el ID de URL de YouTube (funciona con watch?v= y youtu.be/)
                const regExp = /^.*(youtu.be\/|v\/|u\/\w\/|embed\/|watch\?v=|\&v=)([^#\&\?]*).*/;
                const match = url.match(regExp);
                return (match && match[2].length === 11) ? match[2] : null;
            }

            const videoId = getYouTubeId(trailerUrl);

            if (videoId) {
                // Formato de URL de YouTube para incrustar (autoplay=1)
                youtubeIframe.src = `https://www.youtube.com/embed/${videoId}?autoplay=1`;
            } else {
                youtubeIframe.src = ""; // Limpiar si la URL es inválida
            }
        });

        // 5. Detener el video al cerrar el modal 
        detailModal.addEventListener('hide.bs.modal', () => {
            const youtubeIframe = document.getElementById('youtubeIframe');
            youtubeIframe.src = ""; // Esto detiene la reproducción del video y lo limpia
        });
    }


});