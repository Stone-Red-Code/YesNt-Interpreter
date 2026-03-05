const numbers = [];
for (let i = 0; i < 30000; i++) {
  numbers.push(i);
}
let s = 0;
for (let i = 0; i < 30000; i++) {
  s += numbers[i];
}
console.log(s);
