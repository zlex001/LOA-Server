import csv

# Read the file
with open('Library/Design/生物.csv', 'r', encoding='utf-8') as f:
    reader = csv.reader(f)
    rows = list(reader)

# Modify agi column (index 12, 0-based)
header = rows[0]
print(f'Header: {header}')
agi_index = header.index('agi')
print(f'Agi column index: {agi_index}')

count = 0
for i in range(1, len(rows)):
    if len(rows[i]) > agi_index and rows[i][agi_index]:
        old_value = rows[i][agi_index]
        rows[i][agi_index] = '0'
        count += 1
        if i <= 5:
            print(f'Row {i}: {old_value} -> 0')

# Write back
with open('Library/Design/生物.csv', 'w', encoding='utf-8', newline='') as f:
    writer = csv.writer(f)
    writer.writerows(rows)

print(f'Total {count} rows modified, Agi set to 0')

