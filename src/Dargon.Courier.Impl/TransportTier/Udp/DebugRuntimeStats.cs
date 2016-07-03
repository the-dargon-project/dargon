using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dargon.Courier.TransportTier.Udp {
   public class DebugRuntimeStats {
      public static int in_de = 0;
      public static int in_ack = 0;
      public static int in_ack_done = 0;
      public static int in_ann = 0;
      public static int in_ann_out = 0;
      public static int in_pac = 0;
      public static int in_pac_done = 0;
      public static int in_out_ack = 0;
      public static int in_out_ack_done = 0;

      public static int out_mpp = 0;
      public static int out_mpp_done = 0;

      public static int out_nrs = 0;
      public static int out_nrs_done = 0;

      public static int out_rs = 0;
      public static int out_rs_acked = 0;
      public static int out_sent = 0;
      public static int out_rs_done = 0;

      public static int out_ps = 0;
      public static int out_ps_done = 0;


      static DebugRuntimeStats() {
         new Thread(() => {
            while (true) {
               Console.Title = in_de + " ack " + in_ack + " " + in_ack_done + " ann " + in_ann + " " + in_ann_out + " pac " + in_pac + " " + in_pac_done + " ack_out " + in_out_ack + " " + in_out_ack_done +
                               " mpp " + out_mpp + " " + out_mpp_done + " nrs " + out_nrs + " " + out_nrs_done + " rs " + out_rs + " " + out_rs_done + " acked " + out_rs_acked + " sends " + out_sent + " ps " + out_ps + " " + out_ps_done;
               Thread.Sleep(50);
            }
         }) { IsBackground = true }.Start();
      }
   }
}
