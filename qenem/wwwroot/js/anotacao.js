document.addEventListener('DOMContentLoaded', function () {
    const root = document.getElementById('anotacoes-root');
    if (!root) return; // não renderizado -> usuário não autenticado

    const toggle = document.querySelector('.anotacoes-toggle');
    const panel = document.querySelector('.anotacoes-panel');
    const textarea = document.querySelector('.anotacoes-textarea');
    const saveBtn = document.querySelector('.anotacoes-save');
    const savedTag = document.querySelector('.anotacoes-saved');
    const closeBtn = document.querySelector('.anotacoes-close');

    // GET
    fetch('/Anotacoes/Get')
        .then(async resp => {
            if (!resp.ok) {
                const txt = await resp.text();
                console.warn('Falha ao buscar anotações, status:', resp.status, 'body:', txt);
                try {
                    const parsed = JSON.parse(txt);
                    console.warn('Erro recebido do servidor:', parsed.error);
                } catch { /* corpo não-json */ }
                return { anotacoes: '' };
            }
            try {
                return await resp.json();
            } catch (e) {
                console.warn('Resposta não JSON em /Anotacoes/Get', e);
                return { anotacoes: '' };
            }
        })
        .then(data => {
            textarea.value = (data && data.anotacoes) ? data.anotacoes : '';
        })
        .catch(err => {
            console.error('Erro ao buscar anotações', err);
            textarea.value = '';
        });

    toggle.addEventListener('click', function () {
        panel.classList.toggle('open');
    });

    closeBtn?.addEventListener('click', function () {
        panel.classList.remove('open');
    });

    saveBtn.addEventListener('click', function () {
        saveBtn.disabled = true;
        saveBtn.textContent = 'Salvando...';

        const payload = { anotacoes: textarea.value };

        fetch('/Anotacoes/Save', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
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
});