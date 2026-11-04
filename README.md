# Ion8600ClockSync

Sincronismo de horário via **DNP3** para medidores PowerLogic ION8600 (Schneider Electric),
com dashboard de acompanhamento e cadastro de medidores.

O protocolo nativo "ION" desses medidores (porta TCP 7700) é proprietário e confidencial —
a Schneider não publica a especificação de frames, só libera via NDA. Por isso a sincronização
usa **DNP3** (IEEE 1815), que é aberto e documentado, e é suportado pelo ION8600 como protocolo
alternativo de comunicação.

## Arquitetura

```
NtpSyncService (NTP) --5min--> StableClockService (relógio de referência)
                                        |
MedidorSincronismoWorker --60s--> lê horário + bits IIN de cada medidor ativo
                                        |
                        bit NEED_TIME/DEVICE_RESTART? ou já passou o intervalo de sync?
                                        |
                                       sim
                                        v
                 mede delay (FC23) -> escreve horário corrigido (FC2, Group50Var1)
                                        |
                              grava histórico no SQL Server
                                        |
                        dashboard lê o banco, navegador faz polling via /api/medidores/status
```

- [`Dnp3Client`](Ion8600ClockSync/Services/Dnp3/Dnp3Client.cs) + [`Dnp3DataLinkFrame`](Ion8600ClockSync/Services/Dnp3/Dnp3DataLinkFrame.cs) + [`Dnp3ApplicationLayer`](Ion8600ClockSync/Services/Dnp3/Dnp3ApplicationLayer.cs):
  implementação mínima e própria do DNP3 (não usa nenhuma lib de terceiros), cobrindo só o
  necessário: `FC23` (Delay Measurement), `FC1 READ`/`FC2 WRITE` de `Group 50 Var 1` (Time and
  Date), e leitura dos bits IIN (`NEED_TIME`, `DEVICE_RESTART`). Bibliotecas DNP3 gratuitas
  existentes (.NET `opendnp3`, equivalentes em Python) são arquivadas desde 2022 e têm bug
  conhecido no lado Master, ou exigem licença comercial para uso em produção — como o DNP3 é
  protocolo público e documentado (ao contrário do ION nativo), optou-se por uma implementação
  própria, pequena e auditável.
- [`MedidorSincronismoWorker`](Ion8600ClockSync/Services/MedidorSincronismoWorker.cs): a cada
  `AppSettings:CheckIntervalSeconds` (padrão 60s), faz uma leitura leve do relógio de cada
  medidor ativo+habilitado. Dispara a correção completa (medir delay + escrever + reler pra
  conferir) se o medidor sinalizar `NEED_TIME`/`DEVICE_RESTART` nos bits IIN, **ou** se já tiver
  passado `AppSettings:SyncIntervalMinutes` (padrão 360min / 6h) desde o último sync — o que
  vier primeiro.
- [`NtpSyncService`](Ion8600ClockSync/Services/Clock/NtpSyncService.cs) / [`StableClockService`](Ion8600ClockSync/Services/Clock/StableClockService.cs):
  relógio de referência próprio, sincronizado via NTP (independente de qualquer outro serviço).
- Cadastro de medidores (`Pages/Medidores`) e dashboard (`Pages/Index`) em EF Core / SQL Server
  — nenhum IP de medidor fica em código ou configuração, só no banco.

## Descobertas importantes da documentação oficial Schneider

- O CRC do cabeçalho do data link layer do DNP3 **inclui os 2 bytes de sincronismo** (`05 64`)
  — não é só sobre `length+control+dest+source` como várias fontes secundárias sugerem
  ambiguamente. Confirmado contra um frame de referência publicado; sem isso, o medidor
  descarta todo frame silenciosamente (parece erro de rede, mas é bug de CRC).
- `TimeSyncType` (ION Clock Module) controla se a troca DNP3 é em UTC ou hora local — com
  `TimeSyncType = UTC`, o firmware do medidor aplica o deslocamento de fuso só na exibição
  local, não na troca via DNP3.
- Esse medidor **não suporta DNP3 Unsolicited Response** (`Sends Unsolicited Responses: Never`
  no device profile oficial) — por isso a detecção de `NEED_TIME`/`DEVICE_RESTART` é por
  polling, não por notificação espontânea do medidor.

## Configuração

Seção `AppSettings` do `appsettings.json` (ou `AppSettings__<Chave>` em produção, ver `deploy/`):

| Chave | Descrição | Padrão |
|---|---|---|
| `CheckIntervalSeconds` | Intervalo da leitura leve de status (IIN) | `60` |
| `SyncIntervalMinutes` | Intervalo máximo entre sincronismos completos | `360` |
| `MasterAddress` | Endereço DNP3 da nossa própria estação (master) | `1` |
| `DnpTimeoutSeconds` | Timeout de cada operação DNP3 | `10` |

Connection string `ConnectionStrings:Ion8600SyncConnection` via User Secrets (dev) ou
`deploy/ion8600clocksync.env` (produção, nunca commitado — só o `.env.example`).

Cadastro de medidores (IP, porta, endereço DNP, se está ativo/habilitado) é feito pela tela
`/Medidores`, não por arquivo de configuração.

## Rodando localmente

```bash
dotnet run --project Ion8600ClockSync/Ion8600ClockSync.csproj
```

Precisa de um `ConnectionStrings:Ion8600SyncConnection` configurado via
`dotnet user-secrets set` apontando pro banco SQL Server (schema aplicado via
`dotnet ef database update`).

## Testes

```bash
dotnet test
```

Cobre o CRC-16/DNP (contra o valor de referência publicado) e o encode/parse da camada de
aplicação DNP3 (contra um frame de referência publicado), sem precisar de um medidor real.

## Deploy

Ver [`deploy/`](deploy/) — systemd, Nginx e `.env.example` pra CentOS (mesmo padrão usado no
ReportSolarWeb e no UsinaUFVMaravilhaSite), e `deploy/update.sh` pra atualizações de rotina.
