import sys
from collections import Counter

def get_layers(filepath):
    layers = Counter()
    layer_entities = {}
    with open(filepath, 'r', encoding='latin-1', errors='ignore') as f:
        lines = f.readlines()
        
    for i in range(len(lines)):
        if lines[i].strip() == '8':
            layer_name = lines[i+1].strip()
            # go backwards to find the entity type
            j = i
            entity_type = "UNKNOWN"
            while j > 0 and j > i - 50:
                if lines[j].strip() == '0':
                    entity_type = lines[j+1].strip()
                    break
                j -= 1
            if entity_type not in ["SECTION", "TABLE", "LAYER"]:
                layers[layer_name] += 1
                if layer_name not in layer_entities:
                    layer_entities[layer_name] = set()
                layer_entities[layer_name].add(entity_type)

    for layer, count in layers.most_common(100):
        print(f"{layer}: {count} entities. Types: {layer_entities.get(layer)}")

if __name__ == '__main__':
    get_layers(sys.argv[1])
