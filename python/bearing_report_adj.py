import re
import sys
import os

class BearingReportAdj:
    """
    Cleans, corrects, and formats bearings and distances in raw legal description texts.
    Conforms to JEA and land surveyor specifications.
    """

    # Common spelling mistakes in legal descriptions
    SPELL_CHECK_RULES = {
        r"\bcommecement\b": "commencement",
        r"\bbegining\b": "beginning",
        r"\bpoint of begining\b": "Point of Beginning",
        r"\bpoint of commecement\b": "Point of Commencement",
        r"\bpoint of reference\b": "Point of Reference",
        r"\braduis\b": "radius",
        r"\btangent\b": "tangent",
        r"\barc length\b": "arc length",
        r"\bchord bearing\b": "chord bearing",
        r"\bchord distance\b": "chord distance",
        r"\bfeet\b": "feet",
    }

    @staticmethod
    def spell_check(text: str) -> str:
        """
        Corrects common typos in the legal description.
        """
        cleaned = text
        for pattern, replacement in BearingReportAdj.SPELL_CHECK_RULES.items():
            cleaned = re.sub(pattern, replacement, cleaned, flags=re.IGNORECASE)
        return cleaned

    @staticmethod
    def clean_symbology(text: str) -> str:
        """
        Corrects strange symbology in bearings (e.g. *, o, d for degrees).
        """
        # Replace * or o or d in bearings, e.g. N 45*30'30" E or N 45o30'30" E
        # Match N/S followed by digits, then a bad character (*, o, d, deg), then minutes, seconds, E/W
        # Let's do general bad symbol cleanups:
        cleaned = text
        
        # Replace * with ° when between digits, or next to degrees digits
        cleaned = re.sub(r"(\d+)(?:\*|deg|o|d|O)(?=\d{2}['\-\s])", r"\1°", cleaned)
        # Handle cases where degrees symbol is just a space or hyphen, e.g. N 45-30-30 E
        # Standardize hyphens to degree-minute-second symbols in bearings
        # Match: N/S followed by spaces/hyphens and digits
        cleaned = re.sub(r"\b([NSns])\s*(\d+)[\-\s](\d+)[\-\s](\d+)\s*([EWew])\b", r"""\1 \2°\3'\4" \5""", cleaned)
        
        # Clean double quotes and single quotes
        cleaned = cleaned.replace("''", '"').replace("”", '"').replace("’’. ", '"').replace("’", "'").replace("`", "'")
        return cleaned

    @staticmethod
    def ensure_distance_units(text: str) -> str:
        """
        Ensures any distances following a bearing are followed by 'feet' or 'ft'.
        E.g. N 45°30'30" E 166.32 -> N 45°30'30" E 166.32 feet
        """
        # Regex to find a bearing followed by a distance number
        pattern = r"([NSns]\s*\d+°\s*\d+['’]\s*\d+[\"”]\s*[EWew])[\s,]+(\d+(?:\.\d+)?)\b"

        def replace_func(match):
            bearing = match.group(1)
            distance = match.group(2)
            # Inspect what follows in the original text
            following_text = text[match.end():].lstrip()
            # Check if it starts with standard distance unit characters
            if re.match(r"^(?:feet|foot|ft|meters|m|'|\"|ch|chains|varas)\b", following_text, re.IGNORECASE) or following_text.startswith("'") or following_text.startswith('"'):
                return match.group(0) # Keep unchanged
            return f"{bearing} {distance} feet"

        cleaned = re.sub(pattern, replace_func, text, flags=re.IGNORECASE)
        return cleaned

    @staticmethod
    def highlight_key_terms(text: str) -> str:
        """
        Bolds and highlights Point of Beginning, Point of Commencement, and Point of Reference.
        """
        cleaned = text
        # Point of Beginning (or POB)
        cleaned = re.sub(r"\bPoint of Beginning\b", r"<mark>**Point of Beginning**</mark>", cleaned, flags=re.IGNORECASE)
        cleaned = re.sub(r"\bPOB\b", r"<mark>**Point of Beginning**</mark>", cleaned)
        
        # Point of Commencement (or POC)
        cleaned = re.sub(r"\bPoint of Commencement\b", r"<mark>**Point of Commencement**</mark>", cleaned, flags=re.IGNORECASE)
        cleaned = re.sub(r"\bPOC\b", r"<mark>**Point of Commencement**</mark>", cleaned)
        
        # Point of Reference (or POR)
        cleaned = re.sub(r"\bPoint of Reference\b", r"<mark>**Point of Reference**</mark>", cleaned, flags=re.IGNORECASE)
        cleaned = re.sub(r"\bPOR\b", r"<mark>**Point of Reference**</mark>", cleaned)
        
        return cleaned

    @classmethod
    def clean_text(cls, text: str) -> str:
        """
        Runs the full suite of spell-checking, symbology cleaning,
        distance unit checking, and term highlighting.
        """
        t = cls.spell_check(text)
        t = cls.clean_symbology(t)
        t = cls.ensure_distance_units(t)
        t = cls.highlight_key_terms(t)
        return t

def main():
    if len(sys.argv) < 2:
        print("Usage: python3 bearing_report_adj.py <input_file_or_text>")
        return

    input_arg = sys.argv[1]
    if os.path.exists(input_arg):
        with open(input_arg, "r") as f:
            text = f.read()
    else:
        text = input_arg

    cleaned = BearingReportAdj.clean_text(text)
    print("\n--- CLEANED LEGAL DESCRIPTION ---")
    print(cleaned)
    print("---------------------------------\n")

if __name__ == "__main__":
    main()
