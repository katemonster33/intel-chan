using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelChan.StableDiffusion
{
    internal class Txt2ImgRequest
    {
        public int denoising_strength { get; set; } = 0;
        public int firstphase_width { get; set; } = 0;
        public int firstphase_height { get; set; } = 0;
        public string prompt { get; set; }
        public List<string> styles { get; set; } = new List<string>();
        public int seed { get; set; } = -1;
        public int batch_size { get; set; } = 1;
        public int n_iter { get; set; } = 1;
        public int steps { get; set; } = 50;
        public int cfg_scale { get; set; } = 7;
        public int width { get; set; } = 512;
        public int height { get; set; } = 512;
        public bool tiling { get; set; } = false;
        public string negative_prompt { get; set; } = "";
        public string sampler_index { get; set; } = "Euler a";
        public bool send_images { get; set; } = true;
        public bool save_images { get; set; } = false;
    }
}
