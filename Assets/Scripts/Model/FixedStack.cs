using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class FixedStack<T>
{
    private readonly T[] stack;

    // This represents the size of the stack and the next index on which to stack up
    int size;

    public FixedStack(int size) {
        stack = new T[size];
        size = 0;
    }

    public void Push(T newval)
    {
        stack[size] = newval;
        ++size;
    }

    public T Pop()
    {
        var elem = stack[size - 1];
        size--;
        return elem;
    }

    public void Clear() {
        size = 0;
    }

    public bool IsEmpty()
    {
        return size == 0;
    }

    public int Size()
    {
        return size;
    }
}