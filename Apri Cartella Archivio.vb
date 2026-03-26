Imports System.IO

Enum StatoRicerca
    NonTrovata
    TrovataSingola
    Ambigua
    NessunaScelta
End Enum

Public Function NormalizzaRagioneSociale(input As String) As String
    If String.IsNullOrWhiteSpace(input) Then
        Return input
    End If

    Dim ragioneSocialeNormalizzata As String = input.ToLowerInvariant()

    ' Normalizzazione SRL: s.r.l., s r l, srl, ecc.
    ragioneSocialeNormalizzata = Regex.Replace(ragioneSocialeNormalizzata, "\bs[.\s]*r[.\s]*l\b", "srl", RegexOptions.IgnoreCase)

    ' Normalizzazione SNC: s.n.c., s n c, snc, ecc.
    ragioneSocialeNormalizzata = Regex.Replace(ragioneSocialeNormalizzata, "\bs[.\s]*n[.\s]*c\b", "snc", RegexOptions.IgnoreCase)

    ' Rimozione spazi multipli
    ragioneSocialeNormalizzata = Regex.Replace(ragioneSocialeNormalizzata, "\s+", " ").Trim()

    Return ragioneSocialeNormalizzata
End Function

Public Function OttieniPercorsoLettera(ragioneSociale As String) As String
    If String.IsNullOrWhiteSpace(ragioneSociale) Then
        Return Nothing
    End If

    Dim basePath As String = "\\servabc2\archivio\_BANCA DATI CLIENTI\"

    Dim primaLettera As Char = Char.ToUpper(ragioneSociale.Trim()(0))

    Dim percorsoLettera As String = System.IO.Path.Combine(basePath, primaLettera.ToString())

    If Not System.IO.Directory.Exists(percorsoLettera) Then
        Return Nothing
    End If

    Return percorsoLettera
End Function

Public Function TrovaCartelleDitta(ragioneSociale As String, percorsoLettera As String) As List(Of String)

    Dim risultati As New List(Of String)

    Dim nomeNormalizzato As String = NormalizzaRagioneSociale(ragioneSociale)

    Dim cartelle = Directory.GetDirectories(percorsoLettera)

    For Each cartella In cartelle

        Dim nomeCartella As String = Path.GetFileName(cartella)
        Dim nomeCartellaNormalizzato As String = NormalizzaRagioneSociale(nomeCartella)

        If nomeCartellaNormalizzato.Contains(nomeNormalizzato) _
           OrElse nomeNormalizzato.Contains(nomeCartellaNormalizzato) Then

            risultati.Add(cartella)

        End If

    Next

    Return risultati

End Function

Public Class RisultatoCartella
    Public Property Stato As StatoRicerca
    Public Property Percorso As String
    Public Property ConteggioFile As Integer
    Public Property CartelleAmbigue As List(Of String)
End Class

Private Function GetCartellaSalvata(idDitta As Integer) As String
    ' TODO: leggere da DB
    Return Nothing
End Function

Private Sub SalvaCartella(idDitta As Integer, percorso As String)
    ' TODO: salvare su DB
End Sub

Private Sub SalvaNessunaScelta(idDitta As Integer)
    ' TODO: salvare "NESSUNA" su DB
End Sub

Public Function RisolviCartellaDitta(idDitta As Integer, ragioneSociale As String) As RisultatoCartella

    Dim risultato As New RisultatoCartella()

    ' 1. Controllo persistenza
    Dim sceltaSalvata As String = GetCartellaSalvata(idDitta)

    If Not String.IsNullOrEmpty(sceltaSalvata) Then

        If sceltaSalvata = "NESSUNA" Then
            risultato.Stato = StatoRicerca.NessunaScelta
            Return risultato
        End If

        If IO.Directory.Exists(sceltaSalvata) Then
            risultato.Stato = StatoRicerca.TrovataSingola
            risultato.Percorso = sceltaSalvata
            risultato.ConteggioFile = ContaFileRicorsivo(sceltaSalvata)
            Return risultato
        End If

        ' Se il path salvato non esiste più → continua con ricerca normale
    End If

    ' 2. Costruzione percorso lettera
    Dim percorsoLettera As String = OttieniPercorsoLettera(ragioneSociale)

    If String.IsNullOrEmpty(percorsoLettera) Then
        risultato.Stato = StatoRicerca.NonTrovata
        Return risultato
    End If

    ' 3. Ricerca cartelle
    Dim cartelle = TrovaCartelleDitta(ragioneSociale, percorsoLettera)

    If cartelle.Count = 0 Then
        risultato.Stato = StatoRicerca.NonTrovata
        Return risultato
    End If

    If cartelle.Count = 1 Then
        Dim percorso As String = cartelle(0)

        ' Salva automaticamente
        SalvaCartella(idDitta, percorso)

        risultato.Stato = StatoRicerca.TrovataSingola
        risultato.Percorso = percorso
        risultato.ConteggioFile = ContaFileRicorsivo(percorso)

        Return risultato
    End If

    ' 4. Ambiguità
    risultato.Stato = StatoRicerca.Ambigua
    risultato.CartelleAmbigue = cartelle

    Return risultato

End Function

Public Function ContaFileRicorsivo(percorso As String) As Integer

    Dim count As Integer = 0

    Try
        count += IO.Directory.GetFiles(percorso).Length

        For Each dir In IO.Directory.GetDirectories(percorso)
            count += ContaFileRicorsivo(dir)
        Next

    Catch ex As Exception
        ' Ignora cartelle non accessibili
    End Try

    Return count

End Function

Public Property IdDitta As Integer
Public Property Cartelle As List(Of String)

Public Property PercorsoSelezionato As String = Nothing
Public Property NessunaScelta As Boolean = False

Private Sub FrmSelezioneCartella_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    lstCartelle.Items.Clear()

    For Each percorso In Cartelle
        lstCartelle.Items.Add(percorso)
    Next

End Sub

Private Sub btnConferma_Click(sender As Object, e As EventArgs) Handles btnConferma.Click

    If lstCartelle.SelectedItem Is Nothing Then
        MessageBox.Show("Seleziona una cartella.")
        Return
    End If

    PercorsoSelezionato = lstCartelle.SelectedItem.ToString()
    NessunaScelta = False

    Me.DialogResult = DialogResult.OK
    Me.Close()

End Sub

Private Sub btnNessuna_Click(sender As Object, e As EventArgs) Handles btnNessuna.Click

    PercorsoSelezionato = Nothing
    NessunaScelta = True

    Me.DialogResult = DialogResult.OK
    Me.Close()

End Sub

Dim frm As New FrmSelezioneCartella()

frm.IdDitta = idDitta
frm.Cartelle = risultato.CartelleAmbigue

If frm.ShowDialog() = DialogResult.OK Then

    If frm.NessunaScelta Then
        SalvaNessunaScelta(idDitta)

    ElseIf Not String.IsNullOrEmpty(frm.PercorsoSelezionato) Then
        SalvaCartella(idDitta, frm.PercorsoSelezionato)
    End If

    ' Ricarica stato dopo scelta
    Dim nuovoRisultato = RisolviCartellaDitta(idDitta, ragioneSociale)

    ' aggiorna UI
End If

Public Class VoceCartella
    Public Property Nome As String
    Public Property Percorso As String

    Public Overrides Function ToString() As String
        Return Nome
    End Function
End Class