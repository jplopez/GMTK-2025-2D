
Ameba Input System

Is an abstraction layer over Unity's Input System to make it easier to use and more flexible. 
Relies on ScriptableObjects called InputRegistry where you can bind an input with a class, a method within a class or even an Action/lambda function.
This allows easy input reading for small projects and prototyping (single class or lambda) as well as for larger projects (specific classes to bind specific methods).

It is highly recommended to use Unity's auto-generated class Inputaction to take advange of the interfaces defined for each action map. This will prevent your code from breaking when you change the input bindings.


