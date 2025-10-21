using Jint; // https://github.com/sebastienros/jint

const string greetfunc = @"function greet(name) { 
        return 'Hello, ' + name; 
    }";

var engine = new Engine().Execute(greetfunc);

var result = engine.Invoke("greet", "Chris");
Console.WriteLine(result);