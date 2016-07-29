using System;
using System.Linq;

namespace BrainFuck
{
    public class Interpreter
    {
        // Characters that are legal in BrainFuck.
        public const string LegalChars = "<>+-.,[]";

        // Minimum memory size.
        public const int MinimumMemorySize = 5;

        // The code of the program, and the block memory of the program.
        private char[] _codeChar;
        private byte[] _memory;

        // Indexes specifying what memory block we're currently at, what instruction we're currently at, and
        // What loop scope level we're at.
        private int _memoryCounter;
        private int _programCounter;
        private int _loopLevel;

        // Delegates that handle displaying and inputting byte values.
        private Action<byte> _output;
        private Func<byte?> _input;


        private Interpreter(int memorySize) {
            this.ChangeMemorySize(memorySize);
        }

        public Interpreter(Func<byte?> input, Action<byte> output, int memorySize) : this(memorySize) {
            // Assign the input and output methods.
            this._output = output;
            this._input = input;
        }


        public void ResetProgram(int memorySize = -1) {
            // Reset the program and memory counter, and the loop level.
            this._programCounter = -1;
            this._memoryCounter = 0;
            this._loopLevel = 0;

            // Are we changing the memory size?
            if (memorySize != -1) {
                // Yes. Changing the array value will automatically default the 
                // values to 0.
                this.ChangeMemorySize(memorySize);
            } else {
                // No. Clear the memory of the program.
                Array.Clear(this._memory, 0, this._memory.Length);
            }
        }

        private void ChangeMemorySize(int memorySize) {
            // Make sure it's not too small for error messages.
            if (memorySize < MinimumMemorySize) {
                memorySize = MinimumMemorySize;
            }
            // Initialize the size of the program memory.
            this._memory = new byte[memorySize];
        }


        public void Run(string code, bool reset = true) {
            // By default, reset the program in case it was already run once.
            if (reset) {
                this.ResetProgram();
            }

            // Purify the given code, so no illegal instructions exist within the code.
            this._codeChar = code.Where(x => LegalChars.Contains(x)).ToArray();

            // Figure out if the given bracket layout within the code
            // makes sense.
            int bracketLevel = 0;
            for (int i = 0; i < this._codeChar.Length; i++) {

                // Increment the bracket level for every opening bracket.
                if (this._codeChar[i] == '[') {
                    bracketLevel += 1;

                    // Decrement for every closing bracket.
                } else if (this._codeChar[i] == ']') {
                    bracketLevel -= 1;
                }

                // If the current bracket level is below zero, it means we reached an extra closing bracket.
                // The code is not syntactically correct; break.
                if (bracketLevel < 0) {
                    break;
                }
            }

            // If we reached an exra closing bracket, or there weren't enough 
            // closing brackets, the bracket level will be non-zero.
            if (bracketLevel != 0) {
                // If that is the case, overwrite the program with a BrainFuck program that displays
                // the error message 'uneven brackets'.
                this._codeChar = "++++++++++++++++++++++++++++++++>++++++++++[>++++++++++++>+++++++++++>+++++ +++++<<<-]>---.>.>+.<<+.>>.<.<<<.>>>>---.<++++.>-.++.<-------.>++.<<--.-.".ToCharArray();
            }

            // Continue to loop through the entire program until we run out of instructions.
            while (++this._programCounter < this._codeChar.Length) {
                this.HandleInstruction(this._codeChar[this._programCounter]);
            }
        }

        private void HandleInstruction(char instruction) {
            switch (instruction) {
                case '+':
                    // Add one to the current memory block.
                    this._memory[this._memoryCounter] += 1;
                    break;
                case '-':
                    // Subtract one to the current memory block.
                    this._memory[this._memoryCounter] -= 1;
                    break;
                case '>':
                    // Move to the next memory block.
                    this._memoryCounter += 1;

                    // If the index is outside our allowed memory size,
                    // reset the program with an error message.
                    if (this._memoryCounter >= this._memory.Length) {
                        this.ResetProgram();
                        this._codeChar = "++++++++++++++++++++++++++++++++>++++++++++[>++++++++++++>+++++++++++>++++++++++<<<-]>>+.<---.-.<<.>>>.>++.<<<<.>>>--.>-.<.++.+++.<+++++.<<.>>>>---.<---.<----.>-.>++.<<--.<<.>>++.".ToCharArray();
                    }
                    break;
                case '<':
                    // Move to a lower memory block.
                    this._memoryCounter -= 1;

                    // If the index is outside our allowed memory size,
                    // reset the program with an error message.
                    if (this._memoryCounter < 0) {
                        this.ResetProgram();
                        this._codeChar = "++++++++++++++++++++++++++++++++>++++++++++[>++++++++++++>+++++++++++>++++++++++<<<-]>>+.<---.-.<<.>>>.>++.<<<<.>>>--.>-.<.++.+++.<+++++.<<.>>>>---.<---.<----.>-.>++.<<--.<<.>>>--.".ToCharArray();
                    }
                    break;
                case '.':
                    // Output the byte at current memory block.
                    this._output?.Invoke(this._memory[this._memoryCounter]);
                    break;
                case ',':
                    // Assign the value at the current memory block as the next byte in the inputstream.
                    this._memory[this._memoryCounter] = this._input?.Invoke() ?? 0;
                    break;
                case '[': {
                        // Increment the current loop level.
                        this._loopLevel += 1;

                        // Check if we should jump ahead.
                        if (this._memory[this._memoryCounter] == 0) {
                            // We do. Create a temporary indexer to check for the closing bracket at the same
                            // loop level.
                            int pos = this._programCounter;

                            // Create a temporary variable to hold the current pos loop level.
                            int level = this._loopLevel;

                            // We know that the code is syntactically correct, so we know there is a valid
                            // closing bracket. Loop through the code to find it.
                            do {
                                pos += 1;

                                // Increment the loop level if we enter a new loop.
                                if (this._codeChar[pos] == '[') {
                                    level += 1;
                                    // Decrement if we leave a loop.
                                } else if (this._codeChar[pos] == ']') {
                                    level -= 1;
                                }
                                // Continue to loop while we haven't gotten out of the current program loop level.
                            } while (level != this._loopLevel - 1);

                            // Modify the program counter, and the loop level so we can properly continue to execute code.
                            this._programCounter = pos;
                            this._loopLevel -= 1;
                        }
                    }
                    break;
                case ']': {
                        // Create a temporary indexer to check for the opening bracket at the same
                        // loop level.
                        int pos = this._programCounter;

                        // Create a temporary variable to hold the current pos loop level.
                        int level = this._loopLevel;

                        // We know that the code is syntactically correct, so we know there is a valid
                        // opening bracket. Loop through the code to find it.
                        do {
                            pos -= 1;

                            // Decrement the loop level if we enter a new loop.
                            if (this._codeChar[pos] == '[') {
                                level -= 1;
                                // Increment if we leave a loop.
                            } else if (this._codeChar[pos] == ']') {
                                level += 1;
                            }

                            // Continue to loop while we haven't gotten out of the current program loop level.
                        } while (level != this._loopLevel - 1);

                        // Modify the program counter, and the loop level so we can properly continue to execute code.
                        this._programCounter = pos - 1;
                        this._loopLevel -= 1;
                    }
                    break;
            }
        }
    }
}