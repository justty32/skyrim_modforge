import os
import glob

with open("training_list.txt", "w") as out:
    for f in glob.glob("transcripts/*.txt"):
        base = os.path.splitext(os.path.basename(f))[0]
        with open(f, "r") as tf:
            text = tf.read().strip()
        out.write(f"{base}.wav|FemaleYoungEager|en|{text}\n")
print("training_list.txt created.")
