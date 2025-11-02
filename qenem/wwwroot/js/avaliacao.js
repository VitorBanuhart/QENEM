document.addEventListener('DOMContentLoaded', function () {
    const containers = document.querySelectorAll('.estrelas'); // mantemos o seletor original para não alterar views


    // Normaliza o valor que veio do backend (pode ser number, string ou objeto com Avaliacao/avaliacao)
    function normalizeAvaliacao(raw) {
        if (raw == null) return null;

        // se veio um objeto (ex.: { Avaliacao: 3 } ou { avaliacao: 3 })
        if (typeof raw === 'object') {
            if (raw.Avaliacao !== undefined) raw = raw.Avaliacao;
            else if (raw.avaliacao !== undefined) raw = raw.avaliacao;
            else return null;
        }

        const v = Number(raw);
        if (isNaN(v)) return null;
        return v;
    }

    // Mapeia valores antigos (1-5) para novo conjunto (1-3), mas
    // se o valor já estiver na nova escala (1..3) retornamos ele diretamente.
    function mapOldToNew(av) {
        const v = normalizeAvaliacao(av);
        if (v == null) return null;

        // se já é 1..3 (nova escala), devolve direto
        if (v >= 1 && v <= 3) return v;

        // caso seja valor antigo (1..5), aplica mapeamento legado:
        // 1-2 => 1 (Fácil), 3 => 2 (Médio), 4-5 => 3 (Difícil)
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

    // Salva a avaliação no backend. Se o backend retornar o valor salvo, usa-o para marcar o botão correto.
    async function salvarAvaliacao(questaoId, valor, botao) {
        try {
            const payload = { QuestaoId: questaoId, Avaliacao: valor };
            const resp = await fetch('/avaliacao/salvar', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'same-origin',
                body: JSON.stringify(payload)
            });


            if (resp.ok) {
                const json = await resp.json();
                if (json && json.success) {
                    // se o backend retornou o valor salvo, prefira usá-lo (evita discrepância de escala)
                    const saved = json.avaliacao !== undefined ? json.avaliacao : valor;
                    const novo = mapOldToNew(saved);
                    if (novo) {
                        const menu = botao.closest('.avaliacao-menu');
                        if (menu) {
                            const target = menu.querySelector(`.avaliacao-btn[data-value="${novo}"]`);
                            if (target) {
                                marcarAtivo(target);
                                return;
                            }
                        }
                        // fallback para o botão clicado
                        marcarAtivo(botao);
                        return;
                    }

                    // se mapOldToNew retornou null, usa o botão clicado
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
            const response = await fetch(`/avaliacao/verificar?questaoPath=${encodeURIComponent(String(questaoId))}`, { credentials: 'same-origin' });
            if (response.ok) {
                const result = await response.json();

                // Possíveis formatos aceitos:
                // { success: true, avaliacao: 3 }
                // { success: true, avaliacao: { Avaliacao: 3 } }
                if (result && result.success && (result.avaliacao !== null && result.avaliacao !== undefined)) {
                    const novo = mapOldToNew(result.avaliacao);
                    if (novo) {
                        const btn = menu.querySelector(`.avaliacao-btn[data-value="${novo}"]`);
                        if (btn) btn.classList.add('active');
                    }
                }
            } else {
                console.warn('Falha ao verificar avaliação existente', response.status);
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