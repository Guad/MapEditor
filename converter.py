lines = []
with open('input.txt', 'r') as inputFile:
    lines = inputFile.read().splitlines()

output = ""

for line in lines:
    line = line.replace(" ", "")
    split = line.split('=')
    integer = int(split[1], 16)
    if(integer > 0x7FFFFFFF):
        integer -= 0x100000000
    output += split[0] + "=" + str(integer) + "\n"
   
with open('output.txt', 'w') as outputFile:
    outputFile.write(output)