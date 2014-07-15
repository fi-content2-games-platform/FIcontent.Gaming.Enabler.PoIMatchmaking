package com.studiogobo.fi.spatialmatchmaking.servlet;

import java.util.concurrent.ArrayBlockingQueue;
import java.util.concurrent.BlockingQueue;

/**
 * This is probably really just a fiber - more specifically, a ThreadFiber.  Perhaps we should look at using jetlang.
 */
public class JobQueue
{
    public JobQueue(int maxQueueLength)
    {
        commandQueue = new ArrayBlockingQueue<Runnable>(maxQueueLength);
        thread.start();
    }

    public void Enqueue(Runnable command) throws InterruptedException
    {
        commandQueue.put(command);
    }

    public void Quit() throws InterruptedException
    {
        Enqueue(new Runnable()
        {
            @Override
            public void run()
            {
                Thread.currentThread().interrupt();
            }
        });

        thread.join();
    }

    private BlockingQueue<Runnable> commandQueue;

    private Thread thread = new Thread(new Runnable()
    {
        @Override
        public void run()
        {
            while (!Thread.currentThread().isInterrupted())
            {
                Runnable command;
                try
                {
                    command = commandQueue.take();
                }
                catch (InterruptedException e)
                {
                    Thread.currentThread().interrupt();
                    break;
                }

                command.run();
            }
        }
    });
}
