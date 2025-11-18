function greet() {
  const name = Host.inputString();
  Host.outputString(`Hello from JS, ${name}`);
}

module.exports = { greet };