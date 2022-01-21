using System;
using System.Threading;

namespace pj2_sem_2


{
    //Delegate
    public delegate void priceCutEvent(Int32 price);
    

    public class Program
    {

        static Thread ThreadVarCard;

        //Thread List
        static Thread[] t = new Thread[5];
        static Thread[] ClientsT = new Thread[3];//int == # sublients
        static Thread[] orderListT = new Thread[500];
        static tBroker[] ClientsO_Main = new tBroker[3]; //int == # sublients
        static String[] cells = new String[2];

        static Order[] orderList = new Order[0];

        //Buffer List
        static Thread[] orderBuffer = new Thread[500];
        public static Int32 header = 0;
        public static Int32 headerPosS;
        public static Int32 headerPosEnd=0;
        public static Int32 available_buffer = 500;


        


        //Sem & Lock
        static Semaphore semaphore = new Semaphore(2, 2);
        static Semaphore semaphore2 = new Semaphore(2, 2);
        static Semaphore theaterSem = new Semaphore(1, 1); //Ensure the will realease the ticket before 
                                                          //the order comes up
        static Semaphore orderSem = new Semaphore(1, 1);
        static Semaphore sem1 = new Semaphore(1, 1);
        static Semaphore clientSem = new Semaphore(3, 3); 

        static AutoResetEvent t1 = new AutoResetEvent(false);
        static AutoResetEvent t2 = new AutoResetEvent(false);


        private static object _locker = new object();
        private static object _locker2 = new object();



        //Global Var
        public static Int32 t_Amt;
        public static Int32 t_price;
        public static Int32 date = 1;

        //public static Int32 orderPerDay;

        public static Int32 t_brok_price; //The price 
        public static Int32 available_cell = 2;
        public static Int32 ocupied_cell = 0;
        public static Int32 version;

        public static Int32 orderLIndex = 0;

        public static bool firstRun = true;
        public static bool firstOrderIn = false;
        public static bool programEnd = true;

        public static Int32 ClientsNum = 3;

       


        //Other
        public static Random ran = new Random();




        static void processOrder(String source, Int32 order_amt,  Int32 cardNum, Int32 headerPosS, Int32 headerPosEnd, string orderVer, Int32 orderUnitP)
        {


            Order o;


         

            


            if (source.Equals("Theater"))
            {

                for (int i = headerPosS; i < headerPosEnd; i++)
                {



                    /*
                     * 
                     * generates an OrderObject (consisting of multiple values), 
                     * and sends the order to the theater through a MultiCellBuffer.
                     * 
                     * One Order One Thread
                     * 
                     */


                    

                    if (t_Amt < 0)
                    {
                        Console.WriteLine("           !!! SOLD OUT !!!  ");
                    }
                    else
                    {
                        o = new Order();

                        o.version = orderVer;
                        String tmp = " [Order..v" + o.version + " AllOrderNum..#" + i + " (via.Theater)]";


                        //(source, name, carNum, order_amt, order_per_price, order#)
                        o.set("Theater", tmp, cardNum, order_amt, orderUnitP, i);

                        //Sending Order to the OrderList
                        orderList[i] = o;


                        //Start order Thread
                        Theater.orderVarStart(i, cardNum);

                    }

                  


                   

                }


            }
            else
            {
                for (int k = headerPosS; k < headerPosEnd; k++)
                {


                    if (t_Amt < 0)
                    {
                        Console.WriteLine("           !!! SOLD OUT !!!  ");
                    }
                    else
                    {
                        o = new Order();
                        firstOrderIn = true;
                        o.version = orderVer;
                        String tmp = " [Order..v" + o.version + " AllOrderNum..#" + k + " (via.Client)]";

                        o.set("Client", tmp, cardNum, order_amt, orderUnitP, k);
                        orderList[k] = o;

                        Theater.orderVarStart(k, cardNum);
                    }

               

                   
                }
            }






        }

        


        public static void reqOrder(String source, Int32 order_amt)
        {
           

            //Check if the buffer is available for this order
            available_buffer -= order_amt;

            //The start header pos for this order
            headerPosS = headerPosEnd;

            //Reserved header pos for this order
            headerPosEnd += order_amt;

            Int32 currentPrice = t_price;
            string orderV = date + "." + version;

            

            


            if (available_buffer >= 0)
            {

                
                    Int32 tmpCarNum = ran.Next(0, 2000);

                    processOrder(source, order_amt, tmpCarNum, headerPosS, headerPosEnd, orderV, currentPrice);
                

               

            }




        }



        /*
         * 
         * Order Class
         * 
         * 
         */

        public class Order 
        {
            public String sender;
            public String senderId;
            public string version;

            public Int32 cardNo;
            public Int32 ticket_Amt;
            public Int32 per_price;
            public Int32 index_order_in_all;
            




            public void set(String source, String senderId, Int32 inputCardNo, Int32 ticketAmt, Int32 unitPrice, Int32 AllOrderNum)
            {
                sender = source;
                this.senderId = senderId;
                cardNo = inputCardNo;
                ticket_Amt = ticketAmt;
                per_price = unitPrice;
                index_order_in_all = AllOrderNum;
            }

         


           


        }




        public class Theater
        {

            //private static object _locker = new object();
            //private Random ran = new Random();

            //public static event priceCutEvent priceCut;

            public event priceCutEvent priceCut; //delegate initialization **Theater tnotify to sub Clients****

            Int32 currentP;



            public Int32 getPrice()
            {

                currentP = t_price;
                return currentP;

            }

            public void TeaBnessFunc()
            {

                int tmp = ran.Next(0, 3);
                Console.WriteLine("OrderAmt @ Theater.COM: {0}", tmp);
                Program.reqOrder("Theater", tmp);
                
               
               
            }
            public void TeaVarOrderFunc()
            {


                //Theater.orderVarStart();

            }


            public static void verifyOrderFunc(object data)
            {
                Int32 current_t_amt;
                Int32 current_t_p = (int) data;
                Int32 request_t_amt;



                /*
                 * Buffer Cell Structure

                 * 
                */


                ///////////////////////////////////////////////////////////////////////////////////////////////////
                

                Console.WriteLine("                    !!!Theater receives {0}, Joinning Multicell Buffer ReadyQ!!! ", Thread.CurrentThread.Name);



                /*
                 * Lock mechanism to ensure the shared memory is safe
                 * 
                 */

                semaphore.WaitOne();

                ocupied_cell++;

                available_cell--;
                //Console.WriteLine(" ----------Cell Status Start----------");
                //Console.WriteLine(" ------------(Occupied: {0} Available: {1} Buffer Size: 2)----------", ocupied_cell, available_cell);
                //Console.WriteLine(" ------------(Occupied Order: {0})----------", Thread.CurrentThread.Name);





                lock (_locker)

                {

                    /*
                     * Varifying Order
                     * 
                     */
                    //Thread.Sleep(1000);




                    current_t_amt = t_Amt;
                    

                    request_t_amt = ran.Next(1, 20);
                    Int32 oTotal = request_t_amt * current_t_p;



                    t_Amt -= request_t_amt;



                    Console.WriteLine(" ");
                    Console.WriteLine("************************************");
                    Console.WriteLine("----------------------{0} generates SUCCESSFULLY, Starting...", Thread.CurrentThread.Name);


                    Console.WriteLine("----------------------{0} tickets available on Date{2} in {1}", current_t_amt, Thread.CurrentThread.Name,date);

                    Console.WriteLine("----------------------{0} request {1} tickets with TODAY_PRICE ${3}, Available Tickets amt after this order: {2}",
                        Thread.CurrentThread.Name, request_t_amt, t_Amt, t_price);

                   

                    Console.WriteLine("---------------------- Processing {0} ---> AmtReq = {1}, Unit Price: {2} Total = {3}", Thread.CurrentThread.Name, request_t_amt, current_t_p, oTotal);
                    



                    Console.WriteLine("----------------------{0} completed...", Thread.CurrentThread.Name);
                    Console.WriteLine("");

                    Console.WriteLine("************************************");
                    








                }
                ocupied_cell--;
                available_cell++;
             
                semaphore.Release();




                //////////////////////////////////////////////////////////////////////////////////////////////////
               
            }


            /*
             * 
             * 
             * 
             */
           

          
            public void theaterFunc()
            {

                Console.WriteLine("Theater Program Starting");
                


            }

            public void changePrice(Int32 price)
            {
                if (price < t_price) // Notify the sub clients when price drops
                {
                    if(priceCut != null)
                    {
                        
                        priceCut(price);
                    }
                }

                t_price = price;
            }


            public static void orderVarStart(Int32 index, Int32 cardNum)
            {

             


                /*
                 * Credit Card Verification 
                 * 
                 * 1000-2000 is valid Card Num
                 * 
                 * 0-999 is invalid
                 * 
                 */

                if(cardNum >= 1000)
                {

                    orderBuffer[index] = new Thread(Theater.verifyOrderFunc);

                    orderBuffer[index].Name = orderList[index].senderId;
                    orderBuffer[index].Start(orderList[index].per_price);
                    orderBuffer[index].Join();

                }
                else
                {
                    lock (_locker2)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("                               !!!{0} INVALID CARD {1}!!!", orderList[index].senderId, cardNum);
                        Console.ForegroundColor = ConsoleColor.White;
                    }
               
                    
                }


               



            }


         

        }



     


        public class tBroker
        {
           
            public void brokerFun()
            {
               

                
                //Thread Awake
                Console.WriteLine("{0} Thread Awake, TicketAgency{0} has everyday low prices: ${1} each  ", Thread.CurrentThread.Name,t_price);

          

            }

            public void clientBness()
            {
                //Each Agent decides How many ticket each needs.
                Int32 order = ran.Next(0, 3);
                Console.WriteLine(" {0} received {1} NEW orders at this time", Thread.CurrentThread.Name, order);
                Program.reqOrder("Client", order);
            }

            public void p_changed_in_theater(Int32 price)
            {

               

                Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~Boss dropped the price, {0} on SALE @ ${1}", Thread.CurrentThread.Name, price);
                Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~Ticket Broker starts to receive new orders");

                Int32 order = ran.Next(0, 3);
                //Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~Ticket Broker received {0} NEW orders after price changed",order);
                Program.reqOrder("Client", order);
            }

        }



         static void Main(string[] args)
         {


            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("*************************************************************Output Digest************************************************************");
            Console.WriteLine("");
            Console.WriteLine("                                 This Program (Order size per Program: 500) runs with 1 Theater and 3 ticketBrokers preset");
            Console.WriteLine("                                 It simulates the ticket status in 3 dates, 20 times price changes per Day");

            Console.WriteLine("                                 Each day one Theater would release same Amt of tickets of same Price");
            Console.WriteLine("");
            Console.WriteLine("                                 Daily Amt: 500                    Release Price: $200");
            Console.WriteLine("");
            Console.WriteLine("                                 In this program:");
            Console.WriteLine("");
            Console.WriteLine("                                   Eseential Working flow");
            Console.WriteLine("");
            Console.WriteLine("                                              1. Initiate Theater and its Clients (creating thread) ");
            Console.WriteLine("                                              2. Order Object Creating reqorder() --> processOrder()");
            Console.WriteLine("                                              3. orderVarStart(data) to varify the Credit Card and creating varification thread  ");
            Console.WriteLine("                                              4. Theater.verifyOrderFunc(data) prints out varifying process for each order with this format  ");
            Console.WriteLine("");
            Console.WriteLine("                                             -------------------(Order Processing Messages)");
            Console.WriteLine("                                             -------------------(Order Processing Messages)");
            Console.WriteLine("                                             -------------------(Order Processing Messages)");

            Console.WriteLine("");
            Console.WriteLine("                                   Credit Card Payment check and Order Receiver run concurrently");
            Console.WriteLine("                                 If the order buffer is FULL (Buffer Size = 2), the order has to wait for generate Order thread");
            
           
            Console.WriteLine("");
            Console.WriteLine("                                     2 orders received thru [Theater] would generate message:              ");
            Console.WriteLine("");
            Console.WriteLine("                                                               OrderAmt @ Theater.COM: 2             ");
            Console.WriteLine("");
            Console.WriteLine("                                     0 orders received thru [Client #0] would generate message:              ");
            Console.WriteLine("");
            Console.WriteLine("                                                            Client #0 received 0 NEW orders at this time             ");
            Console.WriteLine("");
            Console.WriteLine("                                    Order Name (This is theSecond Order in Date 1, it is requested by Client on Day 1, Second Time price change):              ");
            Console.WriteLine("");
            Console.WriteLine("                                                        [Order..v1.2 AllOrderNum..#2 (via.Client)]             ");


            Console.WriteLine("");
            Console.WriteLine("***************************************************************************************************************************************");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.White;







            programEnd = false;


            /*
             * Theater is the object 
             */
            Theater theater = new Theater();
            tBroker client = new tBroker();

            theater.priceCut += new priceCutEvent(client.p_changed_in_theater);

            Thread theaterThread = new Thread(new ThreadStart(theater.theaterFunc)); //Essential theater Thread
            theaterThread.Start();
            theaterThread.Join();
            //theaterThread.Join();


            //ThreadVarCard = new Thread(new ThreadStart(theater.TeaVarOrderFunc)); //Essential theater Thread
           


            orderList = new Order[500];

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("{0} Ticket Broker Added", ClientsNum);
            Console.WriteLine("  ");
            Console.ForegroundColor = ConsoleColor.White;

            



            ///*
            // * 
            // */
            //for (int p = 0; p < 3; p++)
            //{
            //    tBroker client = new tBroker();
            //    ClientsO_Main[p] = client;
            //    /*
            //     * 
            //     * Each ticket broker contains a callback method (event handler) for the theaters to call when a price-cut event occurs.
            //     * 
            //     */
            //    theater.priceCut += new priceCutEvent(ClientsO_Main[p].p_changed_in_theater);
            //}



            /*
             * ticketBroker object starts as a thread with theaterFunc
             * 
             */

            for (int p = 0; p < ClientsNum; p++)
            {

                ClientsT[p] = new Thread(client.brokerFun);
                ClientsT[p].Name = "Client #" + p;
                ClientsT[p].Start();
            }
            for (int t = 0; t < ClientsNum; t++)
            {

                ClientsT[t].Join();

            }









            //Date
            for (int i = 0; i < 3; i++)
            {

                version = 0;

             
                
                t_Amt = 500;
                t_price =200;

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("DATE {0} INITIAL REALEASE", date);
                Console.WriteLine("Theater Release Tickets: ${0} Available Amt: {1}."
                    , t_price, t_Amt);
                Console.ForegroundColor = ConsoleColor.White;







                //Turning on the System every day

                
                //update ticket price three tiems per Day
                for (int j = 0; j < 20; j++)
                {
                  
                  

                    //Price Change Func

                    Int32 ranP;
                 
                    ranP = ran.Next(50, 200);

                    version++;

                    //Calling Theater/Client BusinessFunc to receive Orders



                    Thread.Sleep(150);

                    /*
                     *    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("DATE {0}--OrderVersion: {2}.{1}", date, version, date);
                    Console.WriteLine("Theater change price {0} times", j + 1);
                    Console.WriteLine("Theater Changed Price: ${0} Available Amt: {1}."
                        , ranP, t_Amt);
                    Console.ForegroundColor = ConsoleColor.White;
                     */



                    for (int p = 0; p < 3; p++)
                    {

                        ClientsT[p] = new Thread(client.brokerFun);
                        ClientsT[p].Name = "Client #" + p;
                        ClientsT[p].Start();
                    }


                    Thread theaterBnessThread = new Thread(new ThreadStart(theater.TeaBnessFunc));
                    theaterBnessThread.Start();
                    theaterBnessThread.Join();

                  


                    for (int p = 0; p < ClientsNum; p++)
                    {

                        ClientsT[p] = new Thread(client.clientBness);
                        ClientsT[p].Name = "Client #" + p;
                        ClientsT[p].Start();
                    }
                    for (int t = 0; t < ClientsNum; t++)
                    {

                        ClientsT[t].Join();

                    }


                  
                    

                    theater.changePrice(ranP);

                 

                    //Theater.priceCut += new priceCutEvent(clients.p_changed_in_theater);


                 





                    //Console.WriteLine(DateTime.Now.ToString("HH:mm:ss tt"));




                    
                    for (int t = 0; t < 3; t++)
                    {

                        ClientsT[t].Join();

                    }

                    /*
                     * Oringinal Order Join
                     * 
                     */







                }


                



                //t_price = ran.Next(10, 40);
                //orderPerDay = 5;

                //Reset the orderBuffer

                Console.WriteLine("Theater sold totally {0} orders on Date {1}",headerPosEnd,date);
                


                Array.Clear(orderBuffer, 0, 500);
                header = 0;

                orderLIndex = 0;
                firstOrderIn = false;



                date++;

            }


            programEnd = true;

        }


    }




   

}
