# https://users.cs.duke.edu/~reif/paper/markas/pub.quad.pdf
# https://www.youtube.com/watch?v=4g33UCV2Pqw
import os
import numpy as np
from PIL import Image, ImageDraw

ERROR_THRESHOLD = 13

def weighted_average(hist):
    """Returns the weighted color average and error from a hisogram of pixles"""
    total = sum(hist)
    value, error = 0, 0
    if total > 0:
        value = sum(i * x for i, x in enumerate(hist)) / total
        error = sum(x * (value - i) ** 2 for i, x in enumerate(hist)) / total
        error = error ** 0.5
    return value, error

def color_from_histogram(image):
    hist = image.histogram()
    """Returns the average rgb color from a given histogram of pixle color counts"""
    r, re = weighted_average(hist[:256])
    g, ge = weighted_average(hist[256:512])
    b, be = weighted_average(hist[512:768])
    e = re * 0.2989 + ge * 0.5870 + be * 0.1140
    return (int(r), int(g), int(b)), e


class Node:
    def __init__(self, image, bbox, depth):
        self.bbox = bbox   # (x0,y0,x1,y1)
        self.depth = depth
        self.children = None   # Should hold 4 childrens
        self.leaf = False

        im = image.crop(bbox)   # Crop image based on bbox
        self.color, self.error = color_from_histogram(im)


class QuadTree():
    def __init__(self, image, max_depth=10):
        self.root = Node(image, image.getbbox(), 0)
        self.width, self.height = image.size
        self.max_depth = max_depth
        self.tree_height = 0

        self._build(self.root, image)

    def _build(self, node, image):
        if (node.depth >= self.max_depth or node.error <= ERROR_THRESHOLD):
            if (node.depth > self.tree_height):
                self.tree_height = node.depth
            node.leaf = True
            return

        self._subdivide(node, image)

        for child in node.children:
            self._build(child, image)
    
    def _subdivide(self, root, image):
        x0,y0,x1,y1 = root.bbox
        mid_x, mid_y = int(x0+(x1-x0)/2), int(y0+(y1-y0)/2)
        #######################################
        # (x0,y0)    (mid_x,y0)    (x1,y0)    #
        # (x0,mid_y) (mid_x,mid_y) (x1,mid_y) #
        # (x0,y1)    (mid_x,y1)    (x1,y1)    #
        #######################################
        node_0 = Node(image, (x0,y0,mid_x,mid_y), root.depth+1) # 2nd quadrant
        node_1 = Node(image, (mid_x,y0,x1,mid_y), root.depth+1) # 1nd quadrant
        node_2 = Node(image, (x0,mid_y,mid_x,y1), root.depth+1) # 3nd quadrant
        node_3 = Node(image, (mid_x,mid_y,x1,y1), root.depth+1) # 4nd quadrant

        root.children = [node_0,node_1,node_2,node_3]

    def _get_leaf_nodes(self, depth):
        def _get_leaf_nodes_recursion(tree, node, depth, func):
            if (node.leaf or node.depth == depth):
                func(node)
            elif node.children is not None:
                for child in node.children:
                    _get_leaf_nodes_recursion(tree, child, depth, func)
        leaf_nodes = []
        _get_leaf_nodes_recursion(self, self.root, depth, leaf_nodes.append)
        return leaf_nodes

    def create_image(self, depth, show_lines=False):
        im = Image.new('RGB', (self.width, self.height))
        draw = ImageDraw.Draw(im)
        draw.rectangle((0,0,self.width,self.height), (0,0,0))

        leaf_nodes = self._get_leaf_nodes(depth)
        for node in leaf_nodes:
            if (show_lines):
                draw.rectangle(node.bbox, fill=node.color, outline=(0,0,0))
            else:
                draw.rectangle(node.bbox, fill=node.color)
        return im

    def create_gif(self, filename, duration=1000, loop=0, show_lines=False):
        gif = []
        final_image = self.create_image(self.tree_height, show_lines=show_lines)

        for i in range(self.tree_height):
            im = self.create_image(i, show_lines=show_lines)
            gif.append(im)
        for i in range(5):
            gif.append(final_image)
        gif[0].save(filename, save_all=True, append_images=gif[1:], duration=duration, loop=loop)

    
        

if __name__ == '__main__':
    image_name = ""   # Image name here

    # load image
    im = Image.open("images/{0}".format(image_name))

    # create quadtree
    qtree = QuadTree(im)

    # save image/gif
    im = qtree.create_image(qtree.tree_height, show_lines=True)
    im.save("output/{0}.png".format(os.path.splitext(image_name)[0]))
    qtree.create_gif("output/{0}.gif".format(os.path.splitext(image_name)[0]), show_lines=True)
    
    # im.show()