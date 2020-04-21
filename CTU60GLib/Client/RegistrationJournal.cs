using System;
using System.Collections.Generic;
using System.Text;

namespace CTU60GLib.Client
{
    public class RegistrationJournal
    {
        private RegistrationJournalPhaseEnum phase;
        private RegistrationSuccesEnum successfullRegistration;
        private List<CollisionTable.CollisionTableItem> closeStations;
        private List<CollisionTable.CollisionTableItem> collisionStations;
        private Exception thrownException;
        public RegistrationJournalPhaseEnum Phase { get { return phase; } }
        public RegistrationSuccesEnum SuccessfullRegistration { get { return successfullRegistration; } }
        public List<CollisionTable.CollisionTableItem> CloseStations { get { return closeStations; } }
        public List<CollisionTable.CollisionTableItem> CollisionStations { get { return collisionStations; } }
        public Exception ThrownException
        {
            get { return thrownException; }
            set
            {
                thrownException = value;
                if()
            }
        }
        public RegistrationJournal()
        {
            phase = RegistrationJournalPhaseEnum.InputValidation;
            successfullRegistration = RegistrationSuccesEnum.Pending;
            
        }
    }
}
