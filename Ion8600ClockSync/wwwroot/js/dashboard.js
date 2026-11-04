(function () {
    "use strict";

    var POLL_MS = 30000;

    function setField(panel, field, text) {
        var el = panel.querySelector('[data-field="' + field + '"]');
        if (el) el.textContent = text;
    }

    function updateMedidorPanel(medidor) {
        var panel = document.querySelector('.medidor-panel[data-medidor-id="' + medidor.id + '"]');
        if (!panel) return;

        var dot = panel.querySelector('[data-field="status-dot"]');
        var statusText = panel.querySelector('[data-field="status-text"]');
        var ultimoSync = medidor.ultimoSync;

        dot.classList.remove("ok", "alert", "idle");

        if (!medidor.dnpHabilitado) {
            dot.classList.add("idle");
            statusText.textContent = "DNP não habilitado";
        } else if (!ultimoSync) {
            dot.classList.add("idle");
            statusText.textContent = "Aguardando leitura";
        } else if (ultimoSync.sucesso) {
            dot.classList.add("ok");
            statusText.textContent = "Sincronizado";
        } else {
            dot.classList.add("alert");
            statusText.textContent = "Falha no sincronismo";
        }

        var horarioAtual = medidor.horarioAtual;
        setField(panel, "horario-atual", horarioAtual && horarioAtual.sucesso ? formatarDataHora(horarioAtual.horarioMedidorUtc) : "--");
        setField(panel, "ultimo-sync", ultimoSync ? formatarDataHora(ultimoSync.dataHoraUtc) : "--");
        setField(panel, "delay", ultimoSync && ultimoSync.delayMedidoMs != null ? ultimoSync.delayMedidoMs + " ms" : "--");
        setField(panel, "desvio", ultimoSync && ultimoSync.desvioMs != null ? ultimoSync.desvioMs + " ms" : "--");
        setField(panel, "resultado", ultimoSync ? (ultimoSync.sucesso ? "OK" : "Falhou") : "--");

        var erroBox = panel.querySelector('[data-field="erro-box"]');
        var erroText = panel.querySelector('[data-field="erro-text"]');
        if (ultimoSync && !ultimoSync.sucesso && ultimoSync.mensagemErro) {
            erroBox.style.display = "";
            erroText.textContent = ultimoSync.mensagemErro;
        } else {
            erroBox.style.display = "none";
        }
    }

    function formatarDataHora(isoString) {
        var d = new Date(isoString);
        return d.toLocaleString("pt-BR", { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit", second: "2-digit" });
    }

    function updateSummary(medidores) {
        var ok = medidores.filter(function (m) { return m.ultimoSync && m.ultimoSync.sucesso; }).length;
        var falha = medidores.filter(function (m) { return m.ultimoSync && !m.ultimoSync.sucesso; }).length;
        var aguardando = medidores.filter(function (m) { return !m.dnpHabilitado; }).length;

        document.getElementById("summary-total").textContent = medidores.length;
        document.getElementById("summary-ok").textContent = ok;
        document.getElementById("summary-falha").textContent = falha;
        document.getElementById("summary-aguardando").textContent = aguardando;
    }

    function updateTopbar(ok) {
        var dot = document.getElementById("conn-dot");
        var text = document.getElementById("conn-text");
        dot.classList.remove("ok", "alert");
        dot.classList.add(ok ? "ok" : "alert");
        text.textContent = ok ? "Conectado" : "Falha ao consultar o servidor";
        document.getElementById("last-update").textContent = "Atualizado " + new Date().toLocaleTimeString("pt-BR");
    }

    function refresh() {
        fetch("/api/medidores/status", { cache: "no-store" })
            .then(function (r) { return r.json(); })
            .then(function (medidores) {
                updateTopbar(true);
                updateSummary(medidores);
                medidores.forEach(updateMedidorPanel);
            })
            .catch(function () {
                updateTopbar(false);
            });
    }

    refresh();
    setInterval(refresh, POLL_MS);
})();
