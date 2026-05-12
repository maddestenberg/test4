import csv
from pathlib import Path

src = Path(r'C:\Users\Linn_\examensarbete 3\test4\BenchmarkDotNet.Artifacts\results\test4.Benchmarks.SerializationBenchmarks-measurements.csv')
dst = src.parent / 'serialization-raw-iterations.csv'

payload_sizes = {
    'Json_Serialize': 723335,
    'Json_Deserialize': 723335,
    'Xml_Serialize': 1301060,
    'Xml_Deserialize': 1301060,
    'Protobuf_Serialize': 323122,
    'Protobuf_Deserialize': 323122,
}

object_count = 2000

with src.open('r', encoding='utf-8', newline='') as f_in, dst.open('w', encoding='utf-8', newline='') as f_out:
    reader = csv.DictReader(f_in, delimiter=';')
    fieldnames = ['Method', 'Operation', 'Iteration', 'ElapsedTimeMs', 'PayloadBytes', 'ObjectCount']
    writer = csv.DictWriter(f_out, fieldnames=fieldnames)
    writer.writeheader()

    for row in reader:
        if row['Measurement_IterationMode'] != 'Workload':
            continue
        if row['Measurement_IterationStage'] != 'Actual':
            continue

        method = row['Target_Method']
        iteration = row['Measurement_IterationIndex']
        elapsed_ns = float(row['Measurement_Value'])
        elapsed_ms = elapsed_ns / 1_000_000
        operation = 'Serialize' if method.endswith('_Serialize') else 'Deserialize'

        writer.writerow({
            'Method': method,
            'Operation': operation,
            'Iteration': iteration,
            'ElapsedTimeMs': f'{elapsed_ms:.6f}',
            'PayloadBytes': payload_sizes.get(method, ''),
            'ObjectCount': object_count,
        })

print('wrote', dst)
