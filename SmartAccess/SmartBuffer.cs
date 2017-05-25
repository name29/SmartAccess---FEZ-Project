using System;
using Microsoft.SPOT;

namespace SmartAccess
{
    class SmartBuffer
    {
        byte[] internalBuffer;
        int step;
        int readPos;
        int writePos;

        //writePos = posizione dove scrivere il prossimo dato
        //readPos = posizione dove leggere il prossimo dato
        //writePos == readPos => buffer vuoto (letto tutto)
        //writePos-1 == readPos => buffer pieno
        public SmartBuffer( int start_size, int _step)
        {
            internalBuffer = new byte[start_size];
            step = _step;
            readPos = 0;
            writePos = 0;
        }

        public void append(byte[] toAppend)
        {
            if ( spaceBetween(writePos,readPos) < toAppend.Length)
            {
                enlarge(toAppend, readPos, internalBuffer.Length + step);
            }

            Boolean after = (readPos > writePos);

            while (true)
            {
                 internalBuffer[writePos] = toAppend[writePos];

                if (!after && readPos >= writePos) break;
                if (writePos == internalBuffer.Length - 1)
                {
                    writePos = 0;
                    after = false;
                }
                else
                {
                    writePos++;
                }
            }
        }

        public byte getAt(int index)
        {
            return internalBuffer[(readPos+index)%internalBuffer.Length];
        }

        public void consume(byte[] buffer, int len)
        {
            if ( internalBuffer.Length - readPos < len)
            {
                throw new Exception("Invalid consume");
            }

            if ( readPos == writePos )
            {
                throw new Exception("No data to consume");
            }

            Boolean after = (readPos > writePos);

            while ( true )
            {
                buffer[readPos] = internalBuffer[readPos];


                if (!after && readPos >= writePos) break;
                if ( readPos == internalBuffer.Length-1 )
                {
                    readPos = 0;
                    after = false;
                }
                else
                {
                    readPos++;
                }
            }
        }


        private int spaceBetween(int a , int b)
        {
            int space = 0;
            if ( a > b )
            {
                space = internalBuffer.Length - a;
                space += b -1;
            }
            else if ( a < b )
            {
                space = b - a -1;
            }

            return space;
        }

        private byte[] enlarge(byte[] original ,int start_copy_from, int new_size)
        {
            if (new_size < original.Length) throw new Exception("Invalid new size");

            byte[] ret = new byte[new_size];

            for (int i = start_copy_from; i < original.Length; i++)
            {
                ret[i] = original[i];
            }

            return ret;
        }
    }
}
