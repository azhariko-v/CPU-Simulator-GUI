using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static CpuSchedulingWinForms.Algorithms;

namespace CpuSchedulingWinForms
{
    public static class Algorithms
    {
        public class Process // process class
        {
            public int id;
            public double arrivalTime;
            public double burstTime;
            public double remainingTime;
            public double priority;
            public double startTime;
            public double completionTime;
            public bool started;

            public Process(int id, double arrival, double burst, double priority = 0)
            {
                this.id = id;
                this.arrivalTime = arrival;
                this.burstTime = burst;
                this.remainingTime = burst;
                this.priority = priority;
                this.started = false;
            }
        }

        private static List<Process> collectProcesses(int numProcesses, bool needsPriority = false)
        {
            List<Process> processes = new List<Process>();//get user input
            for (int i = 0; i < numProcesses; i++)
            {
                double arrival = Convert.ToDouble(Microsoft.VisualBasic.Interaction.InputBox("Enter arrival time for P" + (i + 1), "Arrival Time", "0"));
                double burst = Convert.ToDouble(Microsoft.VisualBasic.Interaction.InputBox("Enter burst time for P" + (i + 1), "Burst Time", "0"));
                double priority = needsPriority ? Convert.ToDouble(Microsoft.VisualBasic.Interaction.InputBox("Enter priority for P" + (i + 1), "Priority", "0")) : 0;
                processes.Add(new Process(i + 1, arrival, burst, priority));
            }
            return processes;
        }

        private static void displayMetrics(List<Process> processes, string algorithmName)
        {
            int n = processes.Count;
            double totalWt = 0, totalTt = 0, totalRt = 0;

            foreach (var p in processes)
            {
                double turnaround = p.completionTime - p.arrivalTime;
                double waiting = turnaround - p.burstTime;
                double response = p.startTime - p.arrivalTime;//calculate metrics

                totalWt += waiting;
                totalTt += turnaround;
                totalRt += response;
            }

            double avgWt = totalWt / n;
            double avgTt = totalTt / n;
            double avgRt = totalRt / n;
            double cpuUtil = (processes.Sum(p => p.burstTime) / (processes.Max(p => p.completionTime) - processes.Min(p => p.arrivalTime))) * 100;
            double throughput = n / (processes.Max(p => p.completionTime) - processes.Min(p => p.arrivalTime));

            MessageBox.Show($"{algorithmName} Performance Metrics:\n\n" +//display metrics
                            $"Average Waiting Time = {avgWt:F2} sec\n" +
                            $"Average Turnaround Time = {avgTt:F2} sec\n" +
                            $"Average Response Time = {avgRt:F2} sec\n" +
                            $"CPU Utilization = {cpuUtil:F2}%\n" +
                            $"Throughput = {throughput:F2} processes/sec", algorithmName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void fcfsAlgorithm(string userInput)//FSFS 
        {
            int numProcesses = Convert.ToInt32(userInput);
            var processes = collectProcesses(numProcesses);

            processes = processes.OrderBy(p => p.arrivalTime).ToList();
            double currentTime = 0;
            foreach (var p in processes)
            {
                p.startTime = Math.Max(currentTime, p.arrivalTime);
                p.completionTime = p.startTime + p.burstTime;
                currentTime = p.completionTime;
            }

            displayMetrics(processes, "First Come First Serve");
        }

        public static void sjfAlgorithm(string userInput) //SJF
        {
            int numProcesses = Convert.ToInt32(userInput);
            var processes = collectProcesses(numProcesses);

            List<Process> ready = new List<Process>();
            double time = 0;
            int completed = 0;

            while (completed < numProcesses)
            {
                ready.AddRange(processes.Where(p => p.arrivalTime <= time && p.remainingTime > 0 && !ready.Contains(p)));
                if (ready.Count > 0)
                {
                    var shortest = ready.OrderBy(p => p.burstTime).First();
                    shortest.startTime = Math.Max(time, shortest.arrivalTime);
                    time = shortest.startTime + shortest.burstTime;
                    shortest.completionTime = time;
                    shortest.remainingTime = 0;
                    ready.Remove(shortest);
                    completed++;
                }
                else
                    time++;
            }

            displayMetrics(processes, "Shortest Job First");
        }

        public static void priorityAlgorithm(string userInput)//PRIORITY
        {
            int numProcesses = Convert.ToInt32(userInput);
            var processes = collectProcesses(numProcesses, true);
            processes = processes.OrderBy(p => p.priority).ThenBy(p => p.arrivalTime).ToList();

            double currentTime = 0;
            foreach (var p in processes)
            {
                p.startTime = Math.Max(currentTime, p.arrivalTime);
                p.completionTime = p.startTime + p.burstTime;
                currentTime = p.completionTime;
            }

            displayMetrics(processes, "Priority Scheduling");
        }

        public static void roundRobinAlgorithm(string userInput)//ROUND ROBIN
        {
            int numProcesses = Convert.ToInt32(userInput);
            var processes = collectProcesses(numProcesses);
            double quantum = Convert.ToDouble(Microsoft.VisualBasic.Interaction.InputBox("Enter time quantum:", "Quantum", "2"));

            Queue<Process> queue = new Queue<Process>();
            double time = 0;
            int completed = 0;

            while (completed < numProcesses)
            {
                foreach (var p in processes.Where(p => p.arrivalTime <= time && p.remainingTime > 0 && !queue.Contains(p)))
                    queue.Enqueue(p);

                if (queue.Count > 0)
                {
                    var current = queue.Dequeue();

                    if (!current.started)
                    {
                        current.started = true;
                        current.startTime = Math.Max(time, current.arrivalTime);
                    }

                    double exec = Math.Min(current.remainingTime, quantum);
                    current.remainingTime -= exec;
                    time += exec;

                    foreach (var p in processes.Where(p => p.arrivalTime <= time && p.remainingTime > 0 && !queue.Contains(p)))
                        queue.Enqueue(p);

                    if (current.remainingTime == 0)
                    {
                        current.completionTime = time;
                        completed++;
                    }
                    else
                    {
                        queue.Enqueue(current);
                    }
                }
                else
                    time++;
            }

            displayMetrics(processes, "Round Robin");
        }

        //NEW ALGORITHMS
        public static void srtfAlgorithm(string userInput)//SRTF
        {
            int numProcesses = Convert.ToInt32(userInput);
            var processes = collectProcesses(numProcesses);
            double time = 0;
            int completed = 0;

            while (completed < numProcesses)
            {
                var ready = processes.Where(p => p.arrivalTime <= time && p.remainingTime > 0)
                                     .OrderBy(p => p.remainingTime).FirstOrDefault();

                if (ready != null)
                {
                    if (!ready.started)
                    {
                        ready.started = true;
                        ready.startTime = Math.Max(time, ready.arrivalTime);
                    }

                    ready.remainingTime--;
                    time++;

                    if (ready.remainingTime == 0)
                    {
                        ready.completionTime = time;
                        completed++;
                    }
                }
                else
                    time++;
            }

            displayMetrics(processes, "Shortest Remaining Time First");
        }

        public static void mlfqAlgorithm(string userInput) //MLFQ
        {
            int numProcesses = Convert.ToInt32(userInput);
            var processes = collectProcesses(numProcesses);
            Queue<Process> q1 = new Queue<Process>();
            Queue<Process> q2 = new Queue<Process>();
            Queue<Process> q3 = new Queue<Process>();

            double time = 0;
            int completed = 0;

            while (completed < numProcesses)
            {
                foreach (var p in processes.Where(p => p.arrivalTime == time && p.remainingTime > 0))
                    q1.Enqueue(p);

                Process current = null;
                int quantum = 0;

                if (q1.Count > 0) { current = q1.Dequeue(); quantum = 4; }
                else if (q2.Count > 0) { current = q2.Dequeue(); quantum = 8; }
                else if (q3.Count > 0) { current = q3.Dequeue(); quantum = (int)current.remainingTime; }

                if (current != null)
                {
                    if (!current.started)
                    {
                        current.started = true;
                        current.startTime = Math.Max(time, current.arrivalTime);
                    }

                    double exec = Math.Min(quantum, current.remainingTime);
                    current.remainingTime -= exec;
                    time += exec;

                    foreach (var p in processes.Where(p => p.arrivalTime <= time && p.remainingTime > 0 && !q1.Contains(p) && !q2.Contains(p) && !q3.Contains(p)))
                        q1.Enqueue(p);

                    if (current.remainingTime == 0)
                    {
                        current.completionTime = time;
                        completed++;
                    }
                    else
                    {
                        if (quantum == 4) q2.Enqueue(current);
                        else q3.Enqueue(current);
                    }
                }
                else
                    time++;
            }

            displayMetrics(processes, "Multi-Level Feedback Queue");
        }
    }
}

