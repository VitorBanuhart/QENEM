$(document).ready(function () {
    // Seletores dos elementos do modal
    const modal = $('#modalContato');
    const form = $('#formContato');
    const btnEnviar = $('#btnEnviarEmail');
    const campoEmail = $('#campoEmail');
    const campoMensagem = $('#campoMensagem');
    const resultadoDiv = $('#resultadoModal');
    const validacaoEmailSpan = $('#validacaoEmail');
    const validacaoMensagemSpan = $('#validacaoMensagem');

    // Novo seletor para o container dos campos
    const formFieldsContainer = $('#form-fields-container');

    function getAntiForgeryToken() {
        return form.find('input[name="__RequestVerificationToken"]').val();
    }

    // Função para limpar e resetar o modal ao fechar
    function limparModal() {
        form.trigger("reset");
        resultadoDiv.html('');
        validacaoEmailSpan.text('');
        validacaoMensagemSpan.text('');

        // Garante que os campos e o botão reapareçam se foram escondidos
        formFieldsContainer.show();
        btnEnviar.show().prop('disabled', false).text('Enviar');
    }

    btnEnviar.on('click', function (e) {
        e.preventDefault();

        resultadoDiv.html('');
        validacaoEmailSpan.text('');
        validacaoMensagemSpan.text('');

        const email = campoEmail.val().trim();
        const mensagem = campoMensagem.val().trim();
        let isValid = true;

        if (!email) {
            validacaoEmailSpan.text('O e-mail é obrigatório.');
            isValid = false;
        } else if (!/^\S+@\S+\.\S+$/.test(email)) {
            validacaoEmailSpan.text('Por favor, insira um e-mail válido.');
            isValid = false;
        }

        if (!mensagem) {
            validacaoMensagemSpan.text('A mensagem é obrigatória.');
            isValid = false;
        }

        if (!isValid) {
            return;
        }

        btnEnviar.prop('disabled', true).text('Enviando...');

        $.ajax({
            type: 'POST',
            url: form.attr('action'),
            headers: { 'RequestVerificationToken': getAntiForgeryToken() },
            data: { Email: email, Mensagem: mensagem }
        }).done(function (response) {
            if (response.success) {
                // Adiciona um espaçamento para a mensagem de sucesso
                resultadoDiv.html('<div class="alert alert-success mt-3">' + response.message + '</div>');

                // 👇 CORREÇÃO AQUI 👇
                // Escondemos apenas o container dos campos, não o form inteiro.
                formFieldsContainer.hide();
                btnEnviar.hide();
            } else {
                resultadoDiv.html('<div class="alert alert-danger">' + response.message + '</div>');
            }
        }).fail(function (xhr) {
            const errorMsg = (xhr.responseJSON && xhr.responseJSON.message)
                ? xhr.responseJSON.message
                : "Ocorreu um erro inesperado.";
            resultadoDiv.html('<div class="alert alert-danger">' + errorMsg + '</div>');
        }).always(function () {
            if (formFieldsContainer.is(":visible")) {
                btnEnviar.prop('disabled', false).text('Enviar');
            }
        });
    });

    // Usa a função de limpeza quando o modal é fechado
    modal.on('hidden.bs.modal', function () {
        limparModal();
    });
});