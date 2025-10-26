document.addEventListener('DOMContentLoaded', function () {
    const todosOsContainersDeEstrelas = document.querySelectorAll('.estrelas');

    // Função para carregar as avaliações existentes
    async function carregarAvaliacoes() {
        for (const container of todosOsContainersDeEstrelas) {
            const questaoId = container.dataset.questaoId;
            try {
                const response = await fetch(`/api/avaliacao/verificar/${questaoId}`);
                if (response.ok) {
                    const result = await response.json();
                    if (result.success && result.avaliacao > 0) {
                        pintarEstrelas(container, result.avaliacao);
                    }
                }
            } catch (error) {
                console.error(`Erro ao buscar avaliação para a questão ${questaoId}:`, error);
            }
        }
    }

    // Função para pintar as estrelas de acordo com a avaliação
    function pintarEstrelas(container, avaliacao) {
        const estrelas = container.querySelectorAll('.estrela');
        estrelas.forEach(estrela => {
            estrela.classList.remove('selecionada');
            if (parseInt(estrela.dataset.valor) <= avaliacao) {
                estrela.classList.add('selecionada');
            }
        });
    }

    // Função para salvar a avaliação
    async function salvarAvaliacao(questaoId, avaliacao) {
        try {
            const response = await fetch('/api/avaliacao/salvar', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ questaoId: questaoId, avaliacao: avaliacao })
            });

            if (response.ok) {
                console.log(`Avaliação ${avaliacao} salva para a questão ${questaoId}`);
            } else {
                console.error('Falha ao salvar avaliação.');
            }
        } catch (error) {
            console.error('Erro ao salvar avaliação:', error);
        }
    }

    // Adiciona os eventos de clique e hover
    todosOsContainersDeEstrelas.forEach(container => {
        const estrelas = container.querySelectorAll('.estrela');

        estrelas.forEach(estrela => {
            estrela.addEventListener('click', function () {
                const avaliacao = parseInt(this.dataset.valor);
                const questaoId = container.dataset.questaoId;
                pintarEstrelas(container, avaliacao);
                salvarAvaliacao(questaoId, avaliacao);
            });

            estrela.addEventListener('mouseover', function () {
                const avaliacaoHover = parseInt(this.dataset.valor);
                estrelas.forEach(e => {
                    e.style.color = parseInt(e.dataset.valor) <= avaliacaoHover ? '#ffc107' : '#ccc';
                });
            });
        });

        container.addEventListener('mouseout', function () {
            // Restaura a cor original baseada na classe 'selecionada'
            const estrelas = this.querySelectorAll('.estrela');
            estrelas.forEach(e => {
                e.style.color = e.classList.contains('selecionada') ? '#ffc107' : '#ccc';
            });
        });
    });

    // Carrega as avaliações existentes ao iniciar a página
    carregarAvaliacoes();
});
