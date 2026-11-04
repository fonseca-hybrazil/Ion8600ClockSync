(function () {
    "use strict";

    var POLL_MS = 30000;

    function refresh() {
        fetch("/api/log", { cache: "no-store" })
            .then(function (r) { return r.json(); })
            .then(function (linhas) {
                var view = document.getElementById("log-view");
                view.textContent = linhas.length === 0 ? "(sem registros ainda)" : linhas.join("\n");
                document.getElementById("last-update").textContent = "Atualizado " + new Date().toLocaleTimeString("pt-BR");
            })
            .catch(function () {
                // Mantém o último conteúdo renderizado em tela em caso de falha passageira.
            });
    }

    setInterval(refresh, POLL_MS);
})();
