// Conteúdo para o arquivo wwwroot/js/site-modal.js
$(document).ready(function () {
    // Seletores dos elementos do modal para evitar repetição
    const modal = $('#modalContato');
    const form = $('#formContato');
    const btnEnviar = $('#btnEnviarEmail');
    const campoMensagemTxt = $('#campoMensagemTxt');
    const campoMensagem = $('#campoMensagem');
    const resultadoDiv = $('#resultadoModal');
    const validacaoSpan = $('#validacaoMensagem');

    function getAntiForgeryToken() {
        // Seletor mais simples e direto para o token
        return form.find('input[name="__RequestVerificationToken"]').val();
    }

    function limparModal() {
        form.trigger("reset");
        resultadoDiv.html('');
        validacaoSpan.text('');
        btnEnviar.show(); // Garante que o botão de enviar esteja visível
    }

    btnEnviar.on('click', function (e) {
        e.preventDefault();

        const mensagem = campoMensagem.val().trim();

        // Limpa feedback anterior
        validacaoSpan.text('');
        resultadoDiv.html('');

        if (!mensagem) {
            validacaoSpan.text('A mensagem é obrigatória.');
            return;
        }

        btnEnviar.prop('disabled', true).text('Enviando...');

        $.ajax({
            type: 'POST',
            url: form.attr('action'),
            headers: {
                'RequestVerificationToken': getAntiForgeryToken()
            },
            data: {
                Mensagem: mensagem
            }
        }).done(function (response) {
            // .done() é executado apenas em caso de sucesso (status 2xx)
            if (response.success) {
                resultadoDiv.html('<div class="alert alert-success">' + response.message + '</div>');
                // Esconde o formulário e o botão de enviar para um feedback mais claro
                campoMensagem.closest('.mb-3').hide();
                btnEnviar.hide();
                
            } else {
                resultadoDiv.html('<div class="alert alert-danger">' + response.message + '</div>');
            }
        }).fail(function (xhr) {
            // .fail() é executado em caso de erro (status 4xx, 5xx, etc.)
            const errorMsg = (xhr.responseJSON && xhr.responseJSON.message) ? xhr.responseJSON.message : "Ocorreu um erro inesperado.";
            resultadoDiv.html('<div class="alert alert-danger">' + errorMsg + '</div>');
        }).always(function () {
            // .always() é executado sempre, independentemente de sucesso ou falha
            btnEnviar.prop('disabled', false).text('Enviar');
        });
    });

    // Usa a função de limpeza quando o modal é fechado
    modal.on('hidden.bs.modal', function () {
        limparModal();
        campoMensagem.show(); // Garante que o campo de mensagem reapareça
    });
});