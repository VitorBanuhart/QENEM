document.addEventListener('DOMContentLoaded', function () {
    const containers = document.querySelectorAll('.estrelas'); // mantemos o seletor original para não alterar views


    // Mapeamento de avaliações antigas (1-5) para novo conjunto (1-3):
    // 1-2 => 1 (Fácil)
    // 3 => 2 (Médio)
    // 4-5 => 3 (Difícil)
    function mapOldToNew(av) {
        if (!av) return null;
        const v = Number(av);
        if (isNaN(v) || v <= 0) return null;
        if (v <= 2) return 1;
        if (v === 3) return 2;
        return 3;
    }

    function criarMenu(questaoId) {
        const menu = document.createElement('div');
        menu.className = 'avaliacao-menu';
        menu.setAttribute('role', 'group');
        menu.setAttribute('aria-label', 'Menu de avaliação');


        const itens = [{ v: 1, txt: 'Fácil' }, { v: 2, txt: 'Médio' }, { v: 3, txt: 'Difícil' }];
        itens.forEach(it => {
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'avaliacao-btn';
            btn.dataset.value = String(it.v);
            btn.textContent = it.txt;
            btn.title = it.txt;


            btn.addEventListener('click', async function () {
                await salvarAvaliacao(questaoId, it.v, btn);
            });


            menu.appendChild(btn);
        });


        return menu;
    }

    async function salvarAvaliacao(questaoId, valor, botao) {
        try {
            const payload = { QuestaoId: questaoId, Avaliacao: valor };
            const resp = await fetch('/api/avaliacao/salvar', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });


            if (resp.ok) {
                const json = await resp.json();
                if (json && json.success) {
                    marcarAtivo(botao);
                    return;
                }
                console.warn('Resposta da API não OK', json);
            } else {
                console.error('Falha ao salvar avaliação', resp.status);
            }
        } catch (err) {
            console.error('Erro ao salvar avaliação', err);
        }
    }

    function marcarAtivo(botao) {
        const menu = botao.closest('.avaliacao-menu');
        if (!menu) return;
        menu.querySelectorAll('.avaliacao-btn').forEach(b => b.classList.remove('active'));
        botao.classList.add('active');
    }

    async function carregarParaContainer(container) {
        const questaoId = container.dataset.questaoId;
        // substitui o HTML das estrelas pelo menu (mantendo o lugar)
        const menu = criarMenu(questaoId);
        container.innerHTML = '';
        container.appendChild(menu);


        // consulta se existe avaliação previamente
        try {
            const response = await fetch(`/api/avaliacao/verificar?questaoPath=${encodeURIComponent(String(questaoId))}`, { credentials: 'same-origin' });
            if (response.ok) {
                const result = await response.json();
                if (result && result.success && (result.avaliacao !== null && result.avaliacao !== undefined)) {
                    const novo = mapOldToNew(result.avaliacao);
                    if (novo) {
                        const btn = menu.querySelector(`.avaliacao-btn[data-value="${novo}"]`);
                        if (btn) btn.classList.add('active');
                    }
                }
            }
        } catch (err) {
            console.error('Erro ao carregar avaliação existente', err);
        }
    }

    // Inicializa todos os containers que já usam .estrelas
    containers.forEach(c => {
        carregarParaContainer(c);
    });


});