Enum StatoRicerca
    NonTrovata
    TrovataSingola
    Ambigua
    NessunaScelta
End Enum

Class RisultatoCartella
    Stato
    Percorso
    ConteggioFile
    CartelleAmbigue (List)
End Class

OnLoadDitta(idDitta, ragioneSociale):

    risultato = RisolviCartellaDitta(idDitta, ragioneSociale)

    Switch risultato.Stato

        Case TrovataSingola:
            MostraIconaAttiva()
            MostraConteggio(risultato.ConteggioFile)

        Case Ambigua:
            MostraIconaAttiva()
            MostraMessaggio("Più cartelle trovate. Seleziona quella corretta")

        Case NessunaScelta:
            MostraIconaDisabilitata()

        Case NonTrovata:
            MostraMessaggio("Nessuna cartella trovata")

OnClickIcona(idDitta, ragioneSociale):

    risultato = RisolviCartellaDitta(idDitta, ragioneSociale)

    Switch risultato.Stato

        Case TrovataSingola:
            ApriExplorer(risultato.Percorso)

        Case Ambigua:
            scelta = ApriFormSelezione(risultato.CartelleAmbigue)

            If scelta.Tipo = "Cartella"
                SalvaCartella(idDitta, scelta.Percorso)

            Else If scelta.Tipo = "Nessuna"
                SalvaNessunaScelta(idDitta)

            RicaricaUI()

        Case NessunaScelta:
            // Non fare nulla (oppure permetti reset manuale)

        Case NonTrovata:
            MostraMessaggio("Nessuna cartella trovata")

Function TrovaCartelleDitta(ragioneSociale, percorsoLettera) -> List(Of String)

    risultati = []

    nomeNormalizzato = NormalizzaFormaSocietaria(ragioneSociale)

    cartelle = DirectoryGetDirectories(percorsoLettera)

    For each cartella in cartelle

        nomeCartella = GetFileName(cartella)
        nomeCartellaNormalizzato = NormalizzaFormaSocietaria(nomeCartella)

        If nomeCartellaNormalizzato == nomeNormalizzato Then
            risultati.Add(cartella)

    End For

    return risultati

Function RisolviCartellaDitta(idDitta, ragioneSociale) -> RisultatoCartella

    risultato = new RisultatoCartella()

    // 1. Controllo persistenza
    sceltaSalvata = GetCartellaSalvata(idDitta)

    If sceltaSalvata != null

        If sceltaSalvata == "NESSUNA"
            risultato.Stato = NessunaScelta
            return risultato

        If DirectoryExists(sceltaSalvata)
            risultato.Stato = TrovataSingola
            risultato.Percorso = sceltaSalvata
            risultato.ConteggioFile = ContaFileRicorsivo(sceltaSalvata)
            return risultato

        // se path non valido → continua

    End If


    // 2. Normalizzazione
    nomeNormalizzato = NormalizzaFormaSocietaria(ragioneSociale)


    // 3. Percorso lettera
    lettera = PrimaLettera(nomeNormalizzato)
    percorsoLettera = BASE_PATH + "\" + lettera

    If Not DirectoryExists(percorsoLettera)
        risultato.Stato = NonTrovata
        return risultato


    // 4. Ricerca cartelle
    cartelle = []

    For each cartella in DirectoryGetDirectories(percorsoLettera)

        nomeCartella = GetFileName(cartella)
        nomeCartellaNormalizzato = NormalizzaFormaSocietaria(nomeCartella)

        If Match(nomeNormalizzato, nomeCartellaNormalizzato)
            cartelle.Add(cartella)

    End For


    // 5. Gestione risultati

    If cartelle.Count == 0
        risultato.Stato = NonTrovata
        return risultato

    If cartelle.Count == 1
        percorso = cartelle[0]

        SalvaCartella(idDitta, percorso)

        risultato.Stato = TrovataSingola
        risultato.Percorso = percorso
        risultato.ConteggioFile = ContaFileRicorsivo(percorso)

        return risultato

    If cartelle.Count > 1
        risultato.Stato = Ambigua
        risultato.CartelleAmbigue = cartelle
        return risultato
End Function

Function MatchEsatto(nomeDB, nomeCartella):

    nomeDBNorm = NormalizzaFormaSocietaria(nomeDB)
    nomeCartellaNorm = NormalizzaFormaSocietaria(nomeCartella)

    Return nomeDBNorm == nomeCartellaNorm

Function NormalizzaFormaSocietaria(input):

    testo = lowercase(input)

    sostituisci pattern "s.r.l varianti" → "srl"
    sostituisci pattern "s.n.c varianti" → "snc"

    rimuovi spazi multipli
    trim

    return testo

Function ContaFileRicorsivo(percorso):

    count = numero file nella cartella

    For each sottocartella
        count += ContaFileRicorsivo(sottocartella)

    return count

Function ApriFormSelezione(listaCartelle):

    mostra lista

    utente può:
        selezionare cartella
        oppure scegliere "nessuna"

    return scelta

Function GetCartellaSalvata(idDitta):

    return percorso OR "NESSUNA" OR null

Function SalvaCartella(idDitta, percorso):

    salva idDitta → percorso

Function SalvaNessunaScelta(idDitta):

    salva idDitta → "NESSUNA"

Function ApriExplorer(percorso):

    avvia explorer.exe con percorso

DB
    IdDitta
    PercorsoCartella (string OR "NESSUNA")

Input
    RagioneSociale

Output
    Stato
    Percorso
    ConteggioFile
    ListaAmbigua

