import sys
from PIL import Image
pre=sys.argv[1]
s=Image.new('RGB',(6*520,2*700))
for i in range(12): s.paste(Image.open(f'{pre}_{i}.png').crop((140,80,660,780)),((i%6)*520,(i//6)*700))
s.save(f'sheet_{pre}.jpg',quality=88)
