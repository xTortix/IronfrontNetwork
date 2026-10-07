namespace Ironfront.Matchmaking.Domain.Enums;

public enum BattleQueueEntryStatus
{
    /*
     * Der Spieler sucht aktiv nach einem passenden Match.
     */
    Queued,

    /*
     * Der Matchmaker hat diesen Spieler einem konkreten BattleMatch
     * zugeordnet. Er darf nicht parallel nochmals queuen.
     */
    Matched,

    /*
     * Der Spieler hat die Suche beendet, bevor er einem Match
     * zugeordnet wurde.
     */
    Cancelled,

    /*
     * Die Queue konnte vor der Matchbildung nicht fortgeführt werden.
     * Beispiel: Der Modus wurde deaktiviert oder die Auswahl war
     * serverseitig nicht mehr gültig.
     */
    Failed,

    /*
     * Das zugehörige Match wurde später vollständig abgeschlossen.
     * Dieser Status wird mit dem späteren Match-End-Lifecycle genutzt.
     */
    Completed
}