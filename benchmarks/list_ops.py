numbers = []
for i in range(30000):
    numbers.append(i)
s = 0
for i in range(30000):
    s += numbers[i]
print(s)
