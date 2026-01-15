

// --- TUNING CONSTANTS (The "Engine" Settings) ---
public static class Config
{
    //***************************************************************
    //CWDecoderEngine
    //***************************************************************
    public const int MaxConsecutiveHits = 2;
    public const int MaxConsecutiveMisses = 6;
   

    //***************************************************************
    //MORSE CLOCK
    //***************************************************************
    // MARK CLASSIFICATION
    // Increased to 2.5 to prevent "Heavy Dots" from becoming "Dashes"
    public const double DASH_THRESHOLD_RATIO = 1.8;         //was 1.9
    public const double DOT_MINIMUM_MS = 24;         // max 40wpm
    public const double MIN_DOT_WINDOW = 0.4;               //compared to currect dotMs
    public const double MAX_DOT_WINDOW = 1.5;               //was 3
    public const double MIN_DASH_WINDOW = 2;
    public const double MAX_DASH_WINDOW = 5;

    // SPACE CLASSIFICATION
    // Increased to 2.4 to prevent "Heavy Spaces" from splitting letters
    public const double ELEMENT_SPACE_MAX_RATIO = 1.6;      //was 2.3
    public const double LETTER_SPACE_MIN_RATIO = 1.9;       //was 2.0
    public const double WORD_SPACE_MIN_RATIO = 5.0;

    // TIMING & DEBOUNCE
    public const double DEBOUNCE_RATIO = 0.22;       //was 0.25
    public const double IDLE_DECODE_RATIO = 4;      // More patient idle timer
    public const int FILTER_COMPENSATION_MS = -4;      //was +4 // Start low and use Calibration tool

    // STATISTICS
    public const int HISTORY_BUCKET_SIZE = 25;     //was 25
    public const double ADAPTATION_BASE_WEIGHT = 0.04;
    
}

public static class ConfigPARIS
{
    //***************************************************************
    //CWDecoderEngine
    //***************************************************************
    public const int MaxConsecutiveHits = 2;
    public const int MaxConsecutiveMisses = 6;


    //***************************************************************
    //MORSE CLOCK
    //***************************************************************
    // MARK CLASSIFICATION
    // Increased to 2.5 to prevent "Heavy Dots" from becoming "Dashes"
    public const double DASH_THRESHOLD_RATIO = 1.9;         //was 1.9
    public const double DOT_MINIMUM_MS = 24;         // max 40wpm
    public const double MIN_DOT_WINDOW = 0.4;               //compared to currect dotMs
    public const double MAX_DOT_WINDOW = 1.5;               //was 3
    public const double MIN_DASH_WINDOW = 2;
    public const double MAX_DASH_WINDOW = 5;

    // SPACE CLASSIFICATION
    // Increased to 2.4 to prevent "Heavy Spaces" from splitting letters
    public const double ELEMENT_SPACE_MAX_RATIO = 1.6;      //was 2.3
    public const double LETTER_SPACE_MIN_RATIO = 2.0;       //was 2.0
    public const double WORD_SPACE_MIN_RATIO = 5.0;

    // TIMING & DEBOUNCE
    public const double DEBOUNCE_RATIO = 0.22;       //was 0.25
    public const double IDLE_DECODE_RATIO = 4;      // More patient idle timer
    public const int FILTER_COMPENSATION_MS = 4;      // Start low and use Calibration tool

    // STATISTICS
    public const int HISTORY_BUCKET_SIZE = 25;     //was 25
    public const double ADAPTATION_BASE_WEIGHT = 0.04;

}