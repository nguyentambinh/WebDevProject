using System;
using QLNSVATC.Models;

namespace QLNSVATC.Helpers
{
    public static class LogHelper
    {
        public static void WriteLog(QLNSVATCEntities db, string action, string user, string description)
        {
            var log = new ACTIVITY_LOG    
            {
                ActionCode = action,
                AcTionTime = DateTime.Now,
                PerformedBy = user,
                Description = description
            };

            db.ACTIVITY_LOG.Add(log); 
            db.SaveChanges();
        }
    }
}
