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
        private string registrationId;
        private Exception thrownException;
        public RegistrationJournalPhaseEnum Phase { get { return phase; } }
        public RegistrationSuccesEnum SuccessfullRegistration { get { return successfullRegistration; } }
        public List<CollisionTable.CollisionTableItem> CloseStations 
        {
            get { return closeStations; }
            set { if(closeStations == null) closeStations = value; }
        }
        public List<CollisionTable.CollisionTableItem> CollisionStations
        {
            get { return collisionStations; }
            set { if(collisionStations==null) collisionStations = value; } 
        }
        public Exception ThrownException
        {
            get { return thrownException; }
            set
            {
                if(thrownException == null) thrownException = value;
                switch (phase)
                {
                    case RegistrationJournalPhaseEnum.InputValidation: 
                    case RegistrationJournalPhaseEnum.Localization: successfullRegistration = RegistrationSuccesEnum.Unsuccessfull;
                        break;
                    case RegistrationJournalPhaseEnum.TechnicalSpecification: successfullRegistration = RegistrationSuccesEnum.UnsuccessfullDraft;
                        break;
                    case RegistrationJournalPhaseEnum.CollissionSummary: successfullRegistration = RegistrationSuccesEnum.UnsuccessfullWaiting;
                        break;
                    case RegistrationJournalPhaseEnum.Published: successfullRegistration = RegistrationSuccesEnum.Successfull;
                        break;
                }
            }
        }

        public string RegistrationId
        {
            get { return registrationId; }
            set { if (registrationId == null) registrationId = value; }
        }

        public RegistrationJournalPhaseEnum NextPhase()
        {
            if (successfullRegistration == RegistrationSuccesEnum.Pending)
            {
                phase += 1;
            }
            else if (successfullRegistration != RegistrationSuccesEnum.Pending)
                throw new NotImplementedException();
            if (phase == RegistrationJournalPhaseEnum.Published)
                successfullRegistration = RegistrationSuccesEnum.Successfull;
            return phase;
        }
        public RegistrationJournal()
        {
            phase = RegistrationJournalPhaseEnum.InputValidation;
            successfullRegistration = RegistrationSuccesEnum.Pending;
            
        }
    }
}
