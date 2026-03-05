function add_one(x) {
  return x + 1;
}
let i = 0;
while (i < 60000) {
  i = add_one(i);
}
console.log(i);
