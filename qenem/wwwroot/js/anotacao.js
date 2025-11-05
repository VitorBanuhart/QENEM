document.addEventListener('DOMContentLoaded', function () {
    const root = document.getElementById('anotacoes-root');
    if (!root) return;

    const toggle = document.querySelector('.anotacoes-toggle');
    const panel = document.querySelector('.anotacoes-panel');
    const textarea = document.querySelector('.anotacoes-textarea');
    const saveBtn = document.querySelector('.anotacoes-save');
    const savedTag = document.querySelector('.anotacoes-saved');
    const closeBtn = document.querySelector('.anotacoes-close');
    const dragHandle = document.querySelector('.drag-handle');

    fetch('/Anotacoes/Get')
        .then(async resp => {
            if (!resp.ok) {
                const txt = await resp.text();
                console.warn('Falha ao buscar anotações, status:', resp.status, 'body:', txt);
                return { anotacoes: '' };
            }
            try { return await resp.json(); } catch { return { anotacoes: '' }; }
        })
        .then(data => { textarea.value = (data && data.anotacoes) ? data.anotacoes : ''; })
        .catch(err => { console.error('Erro ao buscar anotações', err); textarea.value = ''; });

    // Toggle painel aberto/fechado
    toggle.addEventListener('click', function () {
        panel.classList.toggle('open');
        // move foco para textarea quando abrir
        if (panel.classList.contains('open')) {
            setTimeout(() => textarea.focus(), 180);
        }
    });

    closeBtn?.addEventListener('click', function () {
        panel.classList.remove('open');
    });

    // Salvar
    saveBtn.addEventListener('click', function () {
        saveBtn.disabled = true;
        saveBtn.textContent = 'Salvando...';

        const payload = { anotacoes: textarea.value };

        fetch('/Anotacoes/Save', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        })
            .then(async resp => {
                if (!resp.ok) {
                    const body = await resp.text();
                    let msg = body;
                    try { msg = JSON.parse(body).error ?? body; } catch { }
                    throw new Error('Erro ao salvar, status ' + resp.status + ' — ' + msg);
                }
                return resp.json().catch(() => ({ success: true }));
            })
            .then(() => {
                savedTag.classList.add('show');
                setTimeout(() => savedTag.classList.remove('show'), 1400);
            })
            .catch(err => {
                console.error(err);
                alert('Erro ao salvar anotações. Veja console para mais detalhes.');
            })
            .finally(() => {
                saveBtn.disabled = false;
                saveBtn.textContent = 'Salvar';
            });
    });

    // Ctrl+S -> salvar
    textarea.addEventListener('keydown', function (e) {
        if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 's') {
            e.preventDefault();
            saveBtn.click();
        }
    });

    /* Arraste para mover */
    (function enableDrag() {
        if (!dragHandle || !panel) return;
        let isDragging = false;
        let startX = 0, startY = 0, startLeft = 0, startTop = 0;

        dragHandle.addEventListener('mousedown', onDown);
        dragHandle.addEventListener('touchstart', onDown, { passive: true });

        function onDown(e) {
            e.preventDefault();
            isDragging = true;
            const evt = (e.touches && e.touches[0]) || e;
            startX = evt.clientX;
            startY = evt.clientY;

            const rect = panel.getBoundingClientRect();
            panel.style.right = 'auto';
            panel.style.left = rect.left + 'px';
            panel.style.top = rect.top + 'px';
            panel.style.bottom = 'auto';

            startLeft = parseFloat(panel.style.left) || rect.left;
            startTop = parseFloat(panel.style.top) || rect.top;

            document.addEventListener('mousemove', onMove);
            document.addEventListener('mouseup', onUp);
            document.addEventListener('touchmove', onMove, { passive: false });
            document.addEventListener('touchend', onUp);
            panel.classList.add('open');
        }

        function onMove(e) {
            if (!isDragging) return;
            e.preventDefault();
            const evt = (e.touches && e.touches[0]) || e;
            const dx = evt.clientX - startX;
            const dy = evt.clientY - startY;
            let newLeft = startLeft + dx;
            let newTop = startTop + dy;

            const rect = panel.getBoundingClientRect();
            const vw = window.innerWidth;
            const vh = window.innerHeight;
            newLeft = Math.max(8, Math.min(newLeft, vw - rect.width - 8));
            newTop = Math.max(8, Math.min(newTop, vh - rect.height - 8));

            panel.style.left = newLeft + 'px';
            panel.style.top = newTop + 'px';
        }

        function onUp() {
            isDragging = false;
            document.removeEventListener('mousemove', onMove);
            document.removeEventListener('mouseup', onUp);
            document.removeEventListener('touchmove', onMove);
            document.removeEventListener('touchend', onUp);
        }
    })();
});