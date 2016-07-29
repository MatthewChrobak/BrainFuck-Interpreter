# Project Description
A BrainFuck interpreter written in C#.

### A quick example of using the interpreter with the C# Console.
```
  var interpreter = new BrainFuck.Interpreter(
      () => (byte?)Console.ReadLine()[0], 
      (val) => Console.Write((char)val), 
      100
  );

  while (true)
      interpreter.Run(Console.ReadLine());
```
