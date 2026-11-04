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
        var horarioAtual = medidor.horarioAtual;

        dot.classList.remove("ok", "alert", "idle");

        // A bolinha/erro refletem a leitura leve mais recente (a cada poucos segundos), não o
        // último sync completo (que só acontece a cada SyncIntervalMinutes) - senão uma falha
        // pontual fica "presa" na tela até o próximo sync completo, mesmo já tendo normalizado.
        if (!medidor.dnpHabilitado) {
            dot.classList.add("idle");
            statusText.textContent = "DNP não habilitado";
        } else if (!horarioAtual) {
            dot.classList.add("idle");
            statusText.textContent = "Aguardando leitura";
        } else if (horarioAtual.sucesso) {
            dot.classList.add("ok");
            statusText.textContent = "Comunicando";
        } else {
            dot.classList.add("alert");
            statusText.textContent = "Falha na comunicação";
        }

        setField(panel, "horario-atual", horarioAtual && horarioAtual.sucesso ? formatarDataHoraBrasil(horarioAtual.horarioMedidorUtc) : "--");
        setField(panel, "ultimo-sync", ultimoSync ? formatarDataHoraUtc(ultimoSync.dataHoraUtc) : "--");
        setField(panel, "delay", ultimoSync && ultimoSync.delayMedidoMs != null ? ultimoSync.delayMedidoMs + " ms" : "--");
        setField(panel, "desvio", ultimoSync && ultimoSync.desvioMs != null ? ultimoSync.desvioMs + " ms" : "--");
        setField(panel, "resultado", ultimoSync ? (ultimoSync.sucesso ? "OK" : "Falhou") : "--");

        var erroBox = panel.querySelector('[data-field="erro-box"]');
        var erroText = panel.querySelector('[data-field="erro-text"]');
        if (horarioAtual && !horarioAtual.sucesso && horarioAtual.mensagemErro) {
            erroBox.style.display = "";
            erroText.textContent = horarioAtual.mensagemErro;
        } else {
            erroBox.style.display = "none";
        }
    }

    // Sem o "timeZone" explícito o navegador converte pro fuso local do dispositivo que
    // está vendo a tela - por isso as duas variantes abaixo, em vez de uma função genérica.
    function formatarDataHoraUtc(isoString) {
        var d = new Date(isoString);
        return d.toLocaleString("pt-BR", { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit", second: "2-digit", timeZone: "UTC" });
    }

    function formatarDataHoraBrasil(isoString) {
        var d = new Date(isoString);
        return d.toLocaleString("pt-BR", { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit", second: "2-digit", timeZone: "America/Sao_Paulo" });
    }

    function updateSummary(medidores) {
        var ok = medidores.filter(function (m) { return m.dnpHabilitado && m.horarioAtual && m.horarioAtual.sucesso; }).length;
        var falha = medidores.filter(function (m) { return m.dnpHabilitado && m.horarioAtual && !m.horarioAtual.sucesso; }).length;
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
